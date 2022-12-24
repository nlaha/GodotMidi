using Godot;
using System;
using Godot.Collections;

[Tool]
public class MidiImportPlugin : EditorImportPlugin
{
    /// Importer for .midi files.
    /// This importer is used to import MIDI files into Godot.
    
    public override String GetImporterName()
    {
        // The name of the importer.
        return "midi.importer.godotmidi";
    }
    
    public override String GetVisibleName()
    {
        // The name of the importer as it appears in the import dialog.
        return "Standard Midi File (MIDI)";
    }
    
    public override Godot.Collections.Array GetRecognizedExtensions()
    {
        // The list of file extensions that this importer can handle.
        return new Godot.Collections.Array { "midi", "mid" };
    }
    
    public override String GetSaveExtension()
    {
        // The extension that will be used for the imported resource.
        return "res";
    }
    
    public override String GetResourceType()
    {
        // The type of the imported resource.
        return "Animation";
    }
    
    public override int GetPresetCount()
    {
        // The number of presets that this importer supports.
        return 0;
    }
    
    public override String GetPresetName(int preset)
    {
        // The name of the preset at the given index.
        return "Default";
    }
    
    public override Godot.Collections.Array GetImportOptions(int preset)
    {
        // The list of import options that this importer supports.
        return new Godot.Collections.Array { };
    }
    
    public override int Import(String source_file, String save_path, Godot.Collections.Dictionary options, Godot.Collections.Array platform_variants, Godot.Collections.Array gen_files)
    {
        // The actual import function.
        // This function is called when the user clicks on the "Import" button in the import dialog.
        // The source_file parameter is the path to the file that the user wants to import.
        // The save_path parameter is the path where the imported resource should be saved.
        // The options parameter contains the values of the import options.
        // The platform_variants parameter contains the values of the platform variants.
        // The gen_files parameter contains the values of the generated files.
        
        // Create a new MIDI object for conversion to godot readable format.
        MIDI midi = new MIDI();

        GD.Print(save_path);

        // Load the MIDI file into the godot node
        Animation midi_res = midi.LoadFromFile(source_file);
        
        // Save the MIDI resource to disk
        if (midi != null)
        {
            String save_file = save_path + "." + GetSaveExtension();

            Error status = ResourceSaver.Save(save_file, midi_res);
            GD.Print("MIDI resource file saved to: " + save_file);
            GD.Print("Resource save status: " + status);
            return 0;
        }
        
        return 1;
    }
}
