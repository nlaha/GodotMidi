using Godot;
using System;

[Tool]
public class GodotMidi : EditorPlugin
{
    EditorImportPlugin midi_import_plugin;
    
    public override void _EnterTree()
    {
        var script = GD.Load<Script>("res://addons/GodotMidi/MidiManager.cs");
        var texture = GD.Load<Texture>("res://addons/GodotMidi/midi_player.png");
        AddCustomType("MidiManager", "Node", script, texture);
        
        // import plugin
        midi_import_plugin = new MidiImportPlugin();
        AddImportPlugin(midi_import_plugin);
    }
    
    public override void _ExitTree()
    {
        // remove import plugin
        RemoveImportPlugin(midi_import_plugin);
        midi_import_plugin = null;
        
        RemoveCustomType("MidiManager");
    }
}
