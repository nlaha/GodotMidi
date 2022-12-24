using Godot;


[Tool]
public class MidiManager : Node
{
    
    [Export]
    public bool autoPlay = true;
    
    [Export]
    public bool autoPlayMusic = true;
    
    [Signal]
    delegate void NoteEvent(int note, int data, MidiEventNote.NoteType type, int track);
    // Example: EmitSignal(12, 127, MidiEventNote.NoteType.NoteOn, 0);

    private AnimationPlayer _animationPlayer;
    private AudioStreamPlayer _audioStreamPlayer;
    
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
                
                _animationPlayer = ap;
            }
            
            // if we don't have an audio stream player as a child, add one
            if (GetNodeOrNull("AudioStreamPlayer") == null)
            {
                AudioStreamPlayer asp = new AudioStreamPlayer();
                    
                AddChild(asp);
                
                // set owner
                asp.Owner = GetTree().EditedSceneRoot;
                
                _audioStreamPlayer = asp;
            }
        }

        if (_animationPlayer == null)
        {
            // get first animation player child
            _animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        }
        
        if (_audioStreamPlayer == null)
        {
            // get first audio stream player child
            _audioStreamPlayer = GetNodeOrNull<AudioStreamPlayer>("AudioStreamPlayer");
        }
        
        // if auto play is enabled, play the animation
        if (autoPlay)
        {
            _animationPlayer.Play(_animationPlayer.GetAnimationList()[0]);
            if (autoPlayMusic)
            {
                _audioStreamPlayer.Play();
            }
        }
    }
    
    public void NoteEventInput(int note, int data, MidiEventNote.NoteType type, int track)
    {
        EmitSignal(nameof(NoteEvent), note, data, type, track);
    }
}
