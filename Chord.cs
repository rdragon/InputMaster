using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InputMaster.Parsers;
using Newtonsoft.Json;

namespace InputMaster
{
  [JsonConverter(typeof(ChordJsonConverter))]
  public class Chord : IEquatable<Chord>, IEnumerable<Combo>
  {
    private readonly List<Combo> CombosReversed;
    private int HashCode;
    public int Length => CombosReversed.Count;

    public Chord(IEnumerable<Combo> combos)
    {
      CombosReversed = combos.Reverse().ToList();
      foreach (var combo in CombosReversed)
        HashCode = HashCode * 1000000007 + combo.GetHashCode();
    }

    public Chord(int capacity)
    {
      CombosReversed = new List<Combo>(capacity);
    }

    public void InsertAtStart(Combo combo)
    {
      CombosReversed.Add(combo);
      HashCode = HashCode * 1000000007 + combo.GetHashCode();
    }

    public void Clear()
    {
      CombosReversed.Clear();
      HashCode = 0;
    }

    public bool HasPrefix(Chord other)
    {
      if (other.Length > Length)
        return false;
      for (var i = 0; i < other.Length; i++)
        if (other.CombosReversed[other.Length - 1 - i] != CombosReversed[Length - 1 - i])
          return false;
      return true;
    }

    public override int GetHashCode()
    {
      return HashCode;
    }

    public override bool Equals(object obj)
    {
      return Equals(obj as Chord);
    }

    public bool Equals(Chord other)
    {
      if (other == null || other.Length != Length)
        return false;
      for (var i = 0; i < Length; i++)
        if (CombosReversed[i] != other.CombosReversed[i])
          return false;
      return true;
    }

    public static bool operator ==(Chord chord1, Chord chord2)
    {
      if (ReferenceEquals(chord1, null) || ReferenceEquals(chord2, null))
        return ReferenceEquals(chord1, chord2);
      return chord1.Equals(chord2);
    }

    public static bool operator !=(Chord chord1, Chord chord2)
    {
      if (ReferenceEquals(chord1, null) || ReferenceEquals(chord2, null))
        return !ReferenceEquals(chord1, chord2);
      return !chord1.Equals(chord2);
    }

    public override string ToString()
    {
      return string.Join("", CombosReversed.Reverse<Combo>().Select(z => z.ToString()));
    }

    public IEnumerator<Combo> GetEnumerator()
    {
      return CombosReversed.Reverse<Combo>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public bool TestPosition(int index, Combo combo)
    {
      return index < Length && CombosReversed[Length - 1 - index].ModeEquals(combo);
    }

    public bool IsMoreSpecificThan(Chord other)
    {
      return GetComparisonValue().CompareTo(other.GetComparisonValue()) > 0;
    }

    private int GetComparisonValue()
    {
      var anys = CombosReversed.Where(z => z.Input == Input.Any).ToList();
      return anys.Select(z => Helper.CountOnes((int)z.Modifiers)).Sum() - 999 * anys.Count;
    }
  }

  public class ChordJsonConverter : JsonConverter
  {
    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(Chord);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      return Env.Config.DefaultChordReader.CreateChord(new LocatedString(reader.Value.ToString()));
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      writer.WriteValue(value.ToString());
    }
  }
}
