using System;
using System.Collections.Generic;

namespace Latsic.IdServer.Utitlies
{
  public class DictJson
  {
    private bool _changed = false;
    private string _jsonDict;
    private Dictionary<string, dynamic> _dict;

    public DictJson(string jsonDict = "")
    {
      _jsonDict = jsonDict;
      if(jsonDict.Length > 0)
      {
        _dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonDict);
      }
      else
      {
        _dict = new Dictionary<string, dynamic>();   
      }
    }

    public bool IsEmpty
    {
      get
      {
        return _dict.Count == 0;
      }
    }

    public bool HasValue(string key)
    {
      return _dict.ContainsKey(key);
    }

    public void SetValue(string key, dynamic value)
    {
      if(_dict.ContainsKey(key) && _dict[key] == value)
      {
        return;
      }
      _changed = true;
      _dict[key] = value;
    }

    public dynamic GetValue(string key)
    {
      return _dict.ContainsKey(key)
        ? _dict[key]
        : null;
    }

    public bool RemoveValue(string key)
    {
      return _dict.Remove(key);
    }

    public string Json {
      get {
        if(!_changed)
        {
          return _jsonDict;
        }
        _jsonDict = Newtonsoft.Json.JsonConvert.SerializeObject(_dict);
        _changed = false;
        return _jsonDict;
      }
      set {
        _changed = false;
        _jsonDict = value;
        _dict.Clear();
        if(_jsonDict.Length > 0)
        {
          _dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(_jsonDict);
        }
      }
    }
  }
}