using Godot;
using System;
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

        float totalTime = 0;
        for (int trkIdx = 0; trkIdx < header.NumTracks; trkIdx++)
        {
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
            for (int i = 0; i < track.NoteEvents.Count; i++)
            {
                // get event
                MidiEventNote noteEvent = track.NoteEvents[i];
                
                // insert event as key in animation track
                // note dict will store note, data, note type and track
                Dictionary noteDict = new Dictionary();
                noteDict.Add("method", "NoteEventInput");
                noteDict.Add("args", new object[] { noteEvent.Note, noteEvent.Data, noteEvent.EventType, trkIdx });
                
                anim.TrackInsertKey(trkIdx, time, noteDict);
                
                // convert note delta time to microseconds
                double tickDuration = (double)header.Tempo / (double)header.Division;
                double deltaMicroseconds = (double)noteEvent.DeltaTime * tickDuration;
                double deltaSeconds = deltaMicroseconds / 1000000.0;
                time += (float)deltaSeconds;

            }
            totalTime += time;
        }
        
        anim.Length = totalTime;
        
        return anim;
    }
}