using System.Collections.Generic;

public class SolutionStore
{
    // A static dictionary to store key-value pairs where the key is a string and the value is an integer
    private static Dictionary<string, int> _map = new Dictionary<string, int>();

    // Method to set a value in the dictionary for the given key
    public static void SetValue(string key, int value)
    {
        // Check if the key is not already in the dictionary
        if (!_map.ContainsKey(key))
            _map.Add(key, 0); // If not, add the key with a default value of 0

        // Update the value associated with the given key
        _map[key] = value; // Set the new value for the key
    }

    // Method to retrieve the value for a given key
    public static int? GetValue(string key)
    {
        // If the key exists, return the value; otherwise, return 0 (indicating no value found)
        return _map.ContainsKey(key) ? _map[key] : 0;
    }
}
