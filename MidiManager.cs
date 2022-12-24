using Godot;


[Tool]
public class MidiManager : Node
{
    [Signal]
    delegate void NoteEvent(int note, int data, MidiEventNote.NoteType type, int track);
    // Example: EmitSignal(12, 127, MidiEventNote.NoteType.NoteOn, 0);

    // ready
    public override void _Ready()
    {
        if (Engine.EditorHint)
        {
            // if we don't have an animation player as a child, add one
            if (GetNodeOrNull("AnimationPlayer") == null)
            {
                AnimationPlayer ap = new AnimationPlayer();
                    
                AddChild(ap);
                
                // set owner
                ap.Owner = GetTree().EditedSceneRoot;
            }
        }
    }
    
    public void NoteEventInput(int note, int data, MidiEventNote.NoteType type, int track)
    {
        EmitSignal(nameof(NoteEvent), note, data, type, track);
    }
}
