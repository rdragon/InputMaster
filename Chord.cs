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
    private readonly List<Combo> _combosReversed;
    private int _hashCode;
    public int Length => _combosReversed.Count;

    public Chord(IEnumerable<Combo> combos)
    {
      _combosReversed = combos.Reverse().ToList();
      foreach (var combo in _combosReversed)
        _hashCode = _hashCode * 1000000007 + combo.GetHashCode();
    }

    public Chord(int capacity)
    {
      _combosReversed = new List<Combo>(capacity);
    }

    public void InsertAtStart(Combo combo)
    {
      _combosReversed.Add(combo);
      _hashCode = _hashCode * 1000000007 + combo.GetHashCode();
    }

    public void Clear()
    {
      _combosReversed.Clear();
      _hashCode = 0;
    }

    public bool HasPrefix(Chord other)
    {
      if (other.Length > Length)
        return false;
      for (var i = 0; i < other.Length; i++)
        if (other._combosReversed[other.Length - 1 - i] != _combosReversed[Length - 1 - i])
          return false;
      return true;
    }

    public override int GetHashCode()
    {
      return _hashCode;
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
        if (_combosReversed[i] != other._combosReversed[i])
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
      return string.Join("", _combosReversed.Reverse<Combo>().Select(z => z.ToString()));
    }

    public IEnumerator<Combo> GetEnumerator()
    {
      return _combosReversed.Reverse<Combo>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public bool TestPosition(int index, Combo combo)
    {
      return index < Length && _combosReversed[Length - 1 - index].ModeEquals(combo);
    }

    public bool IsMoreSpecificThan(Chord other)
    {
      return GetComparisonValue().CompareTo(other.GetComparisonValue()) > 0;
    }

    private int GetComparisonValue()
    {
      var anys = _combosReversed.Where(z => z.Input == Input.Any).ToList();
      return anys.Select(z => Helper.CountOnes((int)z.Modifiers)).Sum() - 999 * anys.Count;
    }
  }
}
