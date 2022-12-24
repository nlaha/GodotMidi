using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// The type of a midi chunk
/// </summary>
public enum MidiChunkType
{
    Header,
    Track,
    Unknown
}

/// <summary>
/// Represents raw data of a MIDI chunk
/// we don't know what it is yet
/// </summary>
public class RawMidiChunk
{
    public string ChunkId { get; private set; }
    public int ChunkSize { get; private set; }
    public byte[] ChunkData { get; private set; }
    public MidiChunkType ChunkType { get; private set; }

    /// <summary>
    /// Loads a MIDI chunk from a stream of bytes
    /// </summary>
    /// <param name="bytes">the input byte stream</param>
    /// <returns>the original byte stream minus the read data</returns>
    public byte[] LoadFromBytes(byte[] bytes)
    {
        ChunkId = System.Text.Encoding.ASCII.GetString(bytes, 0, 4);
        // we have to use this custom function because chunk size is stored in big endian
        ChunkSize = GodotMidiUtils.ToInt32BigEndian(bytes, 4);
        ChunkData = new byte[ChunkSize];
        System.Array.Copy(bytes, 8, ChunkData, 0, ChunkSize);
        // remove the chunk from the array
        byte[] newBytes = new byte[bytes.Length - ChunkSize - 8];
        System.Array.Copy(bytes, ChunkSize + 8, newBytes, 0, newBytes.Length);

        switch (ChunkId)
        {
            case "MThd": // header
                ChunkType = MidiChunkType.Header;
                break;
            case "MTrk": // track
                ChunkType = MidiChunkType.Track;
                break;
            default:
                // unknown header type
                // this will be ignored
                ChunkType = MidiChunkType.Unknown;
                break;
        }

        // return the remaining bytes
        return newBytes;
    }
}

/// <summary>
/// Base class for MIDI chunks
/// </summary>
public abstract class MidiChunk
{
    public abstract bool ParseChunk(RawMidiChunk raw, ref MidiHeaderChunk header);
}

/// <summary>
/// Represents a MIDI header chunk, used to hold file global data
/// </summary>
public class MidiHeaderChunk : MidiChunk
{
    public enum MidiFileFormat
    {
        SingleTrack = 0,
        MultipleSimultaneousTracks = 1,
        MultipleIndependentTracks = 2
    }
    
    public enum MidiDivisionType
    {
        TicksPerQuarterNote = 0,
        FramesPerSecond = 1
    }
    
    
    public MidiFileFormat FileFormat { get; private set; }
    
    public int NumTracks { get; private set; }
    
    public MidiDivisionType DivisionType { get; private set; }
    public int Division { get; private set; }
    public int Tempo { get; set; }

    public MidiHeaderChunk()
    {
        FileFormat = MidiFileFormat.SingleTrack;
        NumTracks = 0;
        DivisionType = MidiDivisionType.TicksPerQuarterNote;
        Division = 0;
        Tempo = 500000;
    }

    public override bool ParseChunk(RawMidiChunk raw, ref MidiHeaderChunk header)
    {
        if (raw.ChunkType != MidiChunkType.Header)
            return false;
        
        // data section of a header contains 3 16-bit words
        // first word is format
        // 0 - single track, 1 - multiple tracks, 2 - multiple songs
        FileFormat = (MidiFileFormat)GodotMidiUtils.ToInt16BigEndian(raw.ChunkData, 0);
        
        // second word is number of tracks
        NumTracks = GodotMidiUtils.ToInt16BigEndian(raw.ChunkData, 2);
        
        // third word is time division
        // ticks per quarter note or (negative SMPTE format + ticks per frame)
        // if bit 15 is 0, then it's ticks per quarter note
        // if bit 15 is 1, then it's SMPTE format
        
        // we use bit 15 to determine the division type
        // the bytes are in big endian, so we have to shift the byte to the right
        // and then mask it with 0x01 to get the bit
        DivisionType = (MidiDivisionType)((raw.ChunkData[4] >> 7) & 0x01);
        
        // if it's ticks per quarter note, then we just read the value
        if (DivisionType == MidiDivisionType.TicksPerQuarterNote)
        {
            Division = GodotMidiUtils.ToInt16BigEndian(raw.ChunkData, 4);
        }
        else
        {
            // TODO: implement SMPTE format
        }

        return true;
    }
}

/// <summary>
/// Base class for MIDI events
/// </summary>
public class MidiEvent
{   
    
    public int Channel { get; set; }
    
    public int DeltaTime { get; set; }
    
    protected int BytesUsed { get; set; }

    public MidiEvent(int channel, int deltaTime)
    {
        this.Channel = channel;
        this.DeltaTime = deltaTime;
    }
    
    public MidiEvent(MidiEvent other)
    {
        Channel = other.Channel;
        DeltaTime = other.DeltaTime;
        BytesUsed = other.BytesUsed;
    }

    public int GetBytesUsed()
    {
        return BytesUsed;
    }
    
    public override String ToString()
    {
        return "Channel: " + Channel + ", Delta Time: " + DeltaTime;
    }
}

/// <summary>
/// Represents a MIDI note on/off event
/// </summary>
public class MidiEventNote : MidiEvent
{
    public enum NoteType
    {
        NoteOn,
        NoteOff,
        Aftertouch,
        Controller,
        ProgramChange,
        ChannelPressure,
        PitchBend
    }
    
    
    public int Channel { get; private set; }
    
    public int Note { get; private set; }
    
    // will be velocity for note on/off, aftertouch value, controller value, etc.
    
    public int Data { get; private set; }
    
    
    public NoteType EventType { get; private set; }
    
    public MidiEventNote(int channel, int deltaTime, byte[] data, NoteType eventType) : base(channel, deltaTime)
    {
        EventType = eventType;
        
        if (
            eventType == NoteType.NoteOn || 
            eventType == NoteType.NoteOff || 
            eventType == NoteType.Aftertouch ||
            eventType == NoteType.Controller)
        {
            Channel = channel;
            Note = data[1];
            Data = data[2];
            BytesUsed = 2;
        }
        else if (eventType == NoteType.ProgramChange || 
                 eventType == NoteType.ChannelPressure)
        {
            Channel = channel;
            Note = data[1];
            BytesUsed = 1;
        }
        else if (eventType == NoteType.PitchBend)
        {
            Channel = channel;
            Note = data[1];
            Data = data[2];
            BytesUsed = 2;
        }
        else
        {
            GD.PrintErr("Invalid MIDI note event type: " + eventType);
        }
    }

    public override String ToString()
    {
        return base.ToString() + ", Note: " + Note + ", Data: " + Data + ", Event Type " + EventType.ToString();
    }
}

/// <summary>
/// Represents a MIDI system real time event
/// </summary>
public class MidiEventSystem : MidiEvent
{
    public enum MidiSystemEventType
    {
        TimingClock = 0xF8,
        Start = 0xFA,
        Continue = 0xFB,
        Stop = 0xFC,
        ActiveSensing = 0xFE,
        Reset = 0xFF
        
    }
    
    public MidiSystemEventType EventType { get; private set; }
    
    public MidiEventSystem(int deltaTime, byte[] data) : base(0, deltaTime)
    {
        EventType = (MidiSystemEventType)data[0];
        BytesUsed = 1;
    }
    
    public MidiEventSystem(MidiEventSystem other) : base(other)
    {
        EventType = other.EventType;
    }
    
    public override String ToString()
    {
        return base.ToString() + ", Event Type: " + EventType;
    }
}

/// <summary>
/// Represents a MIDI meta event
/// </summary>
public class MidiEventMeta : MidiEvent
{
    public enum MidiMetaEventType
    {
        SequenceNumber = 0x00,
        TextEvent = 0x01,
        CopyRightNotice = 0x02,
        SequenceOrTrackName = 0x03,
        InstrumentName = 0x04,
        Lyric = 0x05,
        Marker = 0x06,
        CuePoint = 0x07,
        EndOfTrack = 0x2F,
        SetTempo = 0x51,
        SMPTEOffset = 0x54,
        TimeSignature = 0x58,
        KeySignature = 0x59,
    }
    
    public MidiMetaEventType EventType { get; private set; }
    public byte[] EventData { get; private set; }
    public int EventDataLength { get; private set; }
    
    public MidiEventMeta(int deltaTime, byte[] data) : base(0, deltaTime)
    {
        // the first byte is always 0xFF
        // the second and third bytes are the event type and length
        EventType = (MidiMetaEventType)data[1];
        EventDataLength = GodotMidiUtils.ToVarIntBigEndian(data, 2, out var bytesUsed);
        EventData = new byte[EventDataLength];
        System.Array.Copy(data, 2 + bytesUsed, EventData, 0, EventDataLength);
        BytesUsed = EventDataLength + bytesUsed + 1;
    }
    
    public MidiEventMeta(MidiEventMeta other) : base(other)
    {
        EventType = other.EventType;
        EventData = new byte[other.EventData.Length];
        System.Array.Copy(other.EventData, EventData, EventData.Length);
        EventDataLength = other.EventDataLength;
    }
    
    public override String ToString()
    {
        return base.ToString() + ", Event Type: " + EventType + ", Event Data Length: " + EventDataLength;
    }
}

/// <summary>
/// Represents a MIDI track chunk
/// </summary>
public class MidiTrackChunk : MidiChunk
{
    public struct MidiTimeSignature
    {
        public int Numerator;
        public int Denominator;
        public int ClocksPerMetronomeClick;
        public int Num32ndNotesPerQuarterNote;
    }
    
    public List<MidiEventNote> NoteEvents { get; private set; }
    public List<MidiEventSystem> SystemEvents { get; private set; }
    public List<MidiEventMeta> MetaEvents { get; private set; }
    
    public MidiTimeSignature TimeSignature { get; private set; }

    public MidiTrackChunk()
    {
        NoteEvents = new List<MidiEventNote>();
        SystemEvents = new List<MidiEventSystem>();
        MetaEvents = new List<MidiEventMeta>();
        // initialize to 4/4
        TimeSignature = new MidiTimeSignature()
        {
            Numerator = 4,
            Denominator = 4,
            ClocksPerMetronomeClick = 24,
            Num32ndNotesPerQuarterNote = 8
        };
    }
    
    public void IngestMetaEvent(MidiEventMeta metaEvent, ref MidiHeaderChunk header)
    {
        // switch on event type
        switch(metaEvent.EventType)
        {
            case MidiEventMeta.MidiMetaEventType.Marker:
                // marker
                // variable length
                // first byte is always 0x06
                // second byte is the length of the text
                // the rest of the bytes are the text
                
                // print this out for now
                GD.Print(System.Text.Encoding.ASCII.GetString(metaEvent.EventData, 2, metaEvent.EventDataLength - 2));
                
                break;
            
            case MidiEventMeta.MidiMetaEventType.SetTempo:
                // set tempo
                // 3 bytes
                // the bytes are the tempo in microseconds per quarter note

                // set tempo of the track
                header.Tempo = GodotMidiUtils.ToInt24BigEndian(metaEvent.EventData, 0);
               
                break;
            
            case MidiEventMeta.MidiMetaEventType.TimeSignature:
                // time signature
                // 4 bytes
                // first byte is always 0x04
                // the rest of the bytes are the time signature
                // the first byte is the numerator
                // the second byte is the denominator (2^x)
                // the third byte is the clocks per metronome click
                // the fourth byte is the number of 32nd notes per quarter note
                
                // set time signature of the track
                MidiTimeSignature timeSignature = new MidiTimeSignature();
                timeSignature.Numerator = metaEvent.EventData[0];
                timeSignature.Denominator = (int)Math.Pow(2, metaEvent.EventData[1]);
                timeSignature.ClocksPerMetronomeClick = metaEvent.EventData[2];
                timeSignature.Num32ndNotesPerQuarterNote = metaEvent.EventData[3];
                TimeSignature = timeSignature;
                
                break;
        }
    }
    
    public override bool ParseChunk(RawMidiChunk raw, ref MidiHeaderChunk header)
    {
        if (raw.ChunkType != MidiChunkType.Track)
            return false;

        // read events until we reach the end of the chunk
        // we have to use a while loop because we don't know how many events there are
        // we can only know when we reach the end of the chunk
        int offset = 0;
        while (offset < raw.ChunkSize)
        {
            // first variable length quantity is delta time
            int deltaTime = GodotMidiUtils.ToVarIntBigEndian(raw.ChunkData, offset, out var bytesUsed);
            offset += bytesUsed;

            // next byte is the event type | channel
            byte eventType = raw.ChunkData[offset];
            offset++;

            // the event type is the first 4 bits of the byte
            // the channel is the last 4 bits
            byte eventCode = (byte)(eventType >> 4);
            byte channel = (byte)(eventType & 0x0F);
            
            // special case for meta events
            if (eventType == 0xFF)
            {
                eventCode = 0xFF;
            }

            byte[] eventData = new byte[raw.ChunkSize - (offset - 1)];
            System.Array.Copy(raw.ChunkData, offset - 1, eventData, 0, eventData.Length - 1);

            // the event code determines the type of the event
            switch (eventCode)
            {
                case 0x08: // note off
                    MidiEventNote noteOffEvent = new MidiEventNote(channel, deltaTime, eventData, MidiEventNote.NoteType.NoteOff);
                    offset += noteOffEvent.GetBytesUsed();
                    NoteEvents.Add(noteOffEvent);
                    break;
                case 0x09: // note on
                    MidiEventNote noteOnEvent = new MidiEventNote(channel, deltaTime, eventData, MidiEventNote.NoteType.NoteOn);
                    offset += noteOnEvent.GetBytesUsed();
                    NoteEvents.Add(noteOnEvent);
                    break;
                case 0x0A: // note aftertouch
                    MidiEventNote noteAftertouchEvent = new MidiEventNote(channel, deltaTime, eventData, MidiEventNote.NoteType.Aftertouch);
                    offset += noteAftertouchEvent.GetBytesUsed();
                    NoteEvents.Add(noteAftertouchEvent);
                    break;
                case 0x0B: // controller
                    MidiEventNote noteControllerEvent = new MidiEventNote(channel, deltaTime, eventData, MidiEventNote.NoteType.Controller);
                    offset += noteControllerEvent.GetBytesUsed();
                    NoteEvents.Add(noteControllerEvent);
                    break;
                case 0x0C: // program change
                    MidiEventNote noteProgramChange = new MidiEventNote(channel, deltaTime, eventData, MidiEventNote.NoteType.ProgramChange);
                    offset += noteProgramChange.GetBytesUsed();
                    NoteEvents.Add(noteProgramChange);
                    break;
                case 0x0D: // channel pressure
                    MidiEventNote noteChannelPressure = new MidiEventNote(channel, deltaTime, eventData, MidiEventNote.NoteType.ChannelPressure);
                    offset += noteChannelPressure.GetBytesUsed();
                    NoteEvents.Add(noteChannelPressure);
                    break;
                case 0x0E: // pitch bend
                    MidiEventNote notePitchBlend = new MidiEventNote(channel, deltaTime, eventData, MidiEventNote.NoteType.PitchBend);
                    offset += notePitchBlend.GetBytesUsed();
                    NoteEvents.Add(notePitchBlend);
                    break;
                case 0x0F: // system event
                    MidiEventSystem systemEvent = new MidiEventSystem(deltaTime, eventData);
                    offset += systemEvent.GetBytesUsed();
                    SystemEvents.Add(systemEvent);
                    break;
                case 0xFF: // meta event
                    MidiEventMeta metaEvent = new MidiEventMeta(deltaTime, eventData);
                    offset += metaEvent.GetBytesUsed();
                    IngestMetaEvent(metaEvent, ref header);
                    MetaEvents.Add(metaEvent);
                    break;
            }
            
        }
        
        return true;
    }
}
