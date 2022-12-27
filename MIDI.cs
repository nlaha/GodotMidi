using Godot;
using System;
using System.Linq;
using Godot.Collections;

/// <summary>
/// A node that contains MIDI data.
/// </summary>
[Tool]
public class MIDI : Resource 
{

    private bool Loaded = false;
    
    [Export] // file selection
    public string FilePath { get; set; }

    public Animation LoadFromFile(String filePath)
    {
        // load midi file from file_path
        
        // get midi file data
        File midiFile = new File();
        midiFile.Open(filePath, File.ModeFlags.Read);
        // get all data (bytes) from file
        // convert ulong to long (because ulong is not supported by GetBuffer)
        byte[] midiData = midiFile.GetBuffer((long)midiFile.GetLen());
        midiFile.Close();
        
        // create output animation
        Animation anim = new Animation();

        // read header chunk
        RawMidiChunk headerChunk = new RawMidiChunk();
        midiData = headerChunk.LoadFromBytes(midiData);
        
        // parse header chunk
        MidiHeaderChunk header = new MidiHeaderChunk();
        header.ParseChunk(headerChunk, ref header);
        
        // print header info
        GD.Print("MIDI format: " + header.FileFormat);
        GD.Print("Number of tracks: " + header.NumTracks);
        GD.Print("Division type: " + header.DivisionType);
        GD.Print("Ticks per quarter note: " + header.Division);
        
        // create animation track for each midi track
        for (int i = 0; i < header.NumTracks; i++)
        {
            // create animation track
            anim.AddTrack(Animation.TrackType.Method);
            anim.TrackSetPath(i, "../MidiManager");
        }
        
        float[] trackTimes = new float[header.NumTracks];
        for (int trkIdx = 0; trkIdx < header.NumTracks; trkIdx++)
        {
            float trackTime = 0;
            // read track chunk
            RawMidiChunk trackChunk = new RawMidiChunk();
            midiData = trackChunk.LoadFromBytes(midiData);
            
            // parse track chunk
            MidiTrackChunk track = new MidiTrackChunk();
            track.ParseChunk(trackChunk, ref header);
            
            // print out events
            //GD.Print("Track " + trkIdx + " events:");
            //GD.Print("Tempo: " + header.Tempo);
            
            // loop through note events
            float time = 0;
            for (int i = 0; i < track.EventPointers.Count; i++)
            {
                // get event
                MidiTrackChunk.MidiEventPointer eventPointer = track.EventPointers[i];

                double deltaTime = 0.0f;
                double tickDuration = (double)header.Tempo / (double)header.Division;

                if (eventPointer.Type == MidiTrackChunk.MidiEventType.Meta)
                {
                    MidiEventMeta metaEvent = track.MetaEvents[eventPointer.Index];
                    
                    // insert as key in animation track
                    // key will contain type and data
                    Dictionary evtDict = new Dictionary();
                    evtDict.Add("method", "MetaEventInput");
                    evtDict.Add("args", new object[] { metaEvent.EventType, metaEvent.EventData, trkIdx });

                    // if it's a tempo change event, update the tempo
                    if (metaEvent.EventType == MidiEventMeta.MidiMetaEventType.SetTempo)
                    {
                        header.Tempo = GodotMidiUtils.ToInt24BigEndian(metaEvent.EventData, 0);
                        tickDuration = (double)header.Tempo / (double)header.Division;
                    }
                }

                if (eventPointer.Type == MidiTrackChunk.MidiEventType.Note)
                {
                    MidiEventNote noteEvent = track.NoteEvents[eventPointer.Index];
                
                    // insert event as key in animation track
                    Dictionary evtDict = new Dictionary();
                    evtDict.Add("method", "NoteEventInput");
                    evtDict.Add("args", new object[] { noteEvent.Note, noteEvent.Data, noteEvent.EventType, trkIdx });
                
                    anim.TrackInsertKey(trkIdx, time, evtDict);
                
                    deltaTime = (double)noteEvent.DeltaTime;
                }

                if (eventPointer.Type == MidiTrackChunk.MidiEventType.System)
                {
                    MidiEventSystem systemEvent = track.SystemEvents[eventPointer.Index];
                    // insert event as key in animation track
                    // note dict will store note, data, note type and track
                    Dictionary evtDict = new Dictionary();
                    evtDict.Add("method", "SystemEventInput");
                    evtDict.Add("args", new object[] { systemEvent.EventType, trkIdx });
                
                    anim.TrackInsertKey(trkIdx, time, evtDict);
                
                    deltaTime = (double)systemEvent.DeltaTime;
                }

                double deltaMicroseconds = (double)deltaTime * tickDuration;
                double deltaSeconds = deltaMicroseconds / 1000000.0;
                time += (float)deltaSeconds;
                
            }
            trackTime += time;
            trackTimes[trkIdx] = trackTime;
        }
        
        anim.Length = trackTimes.Max();
        
        return anim;
    }
}