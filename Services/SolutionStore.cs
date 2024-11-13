using System.Collections.Generic;

public class SolutionStore
{
    private static Dictionary<string, int> _map = new Dictionary<string, int>();

    public static void SetValue(string key, int value)
    {
        if (!_map.ContainsKey(key))
            _map.Add(key, 0);
        _map[key] = value;
    }

    public static int? GetValue(string key)
    {
        return _map.ContainsKey(key) ? _map[key] : 0;
    }
}
