using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InputMaster
{
  /// <summary>
  /// Thread-safe
  /// </summary>
  public class PasswordMatrix
  {
    public int Width { get; }
    public int Height => _matrix.Length / Width;
    private readonly string _matrix;

    public PasswordMatrix(string prettyMatrix)
    {
      (_matrix, Width) = StripMatrix(prettyMatrix);
    }

    public static async Task<PasswordMatrix> GetPasswordMatrixAsync()
    {
      var stateHandler = Env.StateHandlerFactory.Create(new MyState(), nameof(PasswordMatrix),
        StateHandlerFlags.UseCipher | StateHandlerFlags.UserEditable | StateHandlerFlags.Exportable);
      var state = await stateHandler.LoadAndSaveAsync();
      return new PasswordMatrix(state.Matrix);
    }

    public MatrixPassword CreateRandomMatrixPassword(int length)
    {
      var blueprint = new PasswordBlueprint
      {
        Length = Env.Config.MatrixPasswordLength
      };
      (var shape, var direction) = GetRandomShapeDirection();
      blueprint.Shape = shape;
      blueprint.Direction = direction;
      SetRandomLocation(blueprint);
      return new MatrixPassword(GetPasswordValue(blueprint), blueprint);
    }

    public string GetPasswordValue(PasswordBlueprint blueprint)
    {
      var path = GetPath(blueprint).ToList();
      if (path.Any(z => Math.Min(z.X, z.Y) < 0 || Width <= z.X || Height <= z.Y))
        return null;
      return new string(path.Select(z => _matrix[z.Y * Width + z.X]).ToArray());
    }

    public IEnumerable<PasswordBlueprint> GetAllBlueprints(int length)
    {
      var blueprint = new PasswordBlueprint
      {
        Length = length
      };
      for (int x = 0; x < Width; x++)
      {
        blueprint.X = x;
        for (int y = 0; y < Height; y++)
        {
          blueprint.Y = y;
          foreach ((var shape, var direction) in ListAllShapeDirections())
          {
            blueprint.Shape = shape;
            blueprint.Direction = direction;
            yield return blueprint;
          }
        }
      }
    }

    public Task<PasswordBlueprint> GetBlueprintAsync(string password)
    {
      return Task.Run(() => GetBlueprint(password));
    }

    private PasswordBlueprint GetBlueprint(string password)
    {
      if (Regex.IsMatch(password, "[^a-z]"))
        return null;
      foreach (var blueprint in GetAllBlueprints(password.Length))
        if (GetPasswordValue(blueprint) == password)
          return blueprint;
      return null;
    }

    private void SetRandomLocation(PasswordBlueprint blueprint)
    {
      for (int i = 0; i < 9999; i++)
      {
        blueprint.X = Helper.GetRandomInt(Width);
        blueprint.Y = Helper.GetRandomInt(Height);
        if (GetPasswordValue(blueprint) == null)
          continue;
        return;
      }
      throw new ArgumentException("Possible infinite loop detected.");
    }

    private static IEnumerable<Vec> GetPath(PasswordBlueprint blueprint)
    {
      return GetEndlessPath(blueprint).Take(blueprint.Length);
    }

    private static IEnumerable<Vec> GetEndlessPath(PasswordBlueprint blueprint)
    {
      var v = new Vec(blueprint.X, blueprint.Y);
      yield return v;
      var d1 = new Vec(blueprint.Direction[0]);
      var d2 = 1 < blueprint.Direction.Length ? new Vec(blueprint.Direction[1]) : (Vec?)null;
      if (blueprint.Shape == BlueprintShape.Straight)
      {
        while (true)
        {
          v = v.Add(d1);
          if (d2.HasValue)
            v = v.Add(d2.Value);
          yield return v;
        }
      }
      if (!d2.HasValue)
        throw new ArgumentException($"Expecting two direction for shape '{blueprint.Shape}'.");
      var a = d1;
      var b = d2.Value;
      if (blueprint.Shape == BlueprintShape.Stairs)
      {
        while (true)
        {
          v = v.Add(a);
          (a, b) = (b, a);
          yield return v;
        }
      }
      if (blueprint.Shape == BlueprintShape.L)
      {
        v = v.Add(a);
        yield return v;
        while (true)
        {
          v = v.Add(b);
          yield return v;
        }
      }
      var steps = 1;
      while (true)
      {
        for (int i = 0; i < steps; i++)
        {
          v = v.Add(a);
          yield return v;
        }
        for (int i = 0; i < steps; i++)
        {
          v = v.Add(b);
          yield return v;
        }
        (a, b) = (a.Multiply(-1), b.Multiply(-1));
        steps++;
      }
    }

    private static (BlueprintShape shape, string direction) GetRandomShapeDirection()
    {
      var list = ListAllShapeDirections().ToList();
      return list[Helper.GetRandomInt(list.Count)];
    }

    private static IEnumerable<(BlueprintShape shape, string direction)> ListAllShapeDirections()
    {
      var dirs = "ENWS";
      foreach (var a in dirs)
      {
        yield return (BlueprintShape.Straight, $"{a}");
        var i = dirs.IndexOf(a);
        foreach (var b in new char[] { dirs[(i + 1) % 4], dirs[(i + 3) % 4] })
        {
          foreach (var shape in Enum.GetValues(typeof(BlueprintShape)).Cast<BlueprintShape>())
          {
            if (shape == BlueprintShape.Straight)
              continue;
            yield return (shape, $"{a}{b}");
          }
        }
      }
    }

    private static (string, int) StripMatrix(string prettyMatrix)
    {
      var lines = Regex.Replace(prettyMatrix, "[^a-z\n]", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
      if (lines.Count == 0)
        throw new ArgumentException("Password matrix cannot be empty.");
      var width = lines[0].Length;
      if (lines.Any(z => z.Length != width))
        throw new ArgumentException("Invalid password matrix input. All rows should be equal in length.");
      return (string.Join("", lines), width);
    }

    private struct Vec
    {
      public int X { get; }
      public int Y { get; }

      public Vec(int x, int y)
      {
        X = x;
        Y = y;
      }

      public Vec(char direction)
      {
        switch (direction)
        {
          case 'N': X = 0; Y = -1; break;
          case 'S': X = 0; Y = 1; break;
          case 'E': X = 1; Y = 0; break;
          case 'W': X = -1; Y = 0; break;
          default: throw new ArgumentException($"Unknown direction '{direction}'.");
        }
      }

      public Vec Add(Vec v)
      {
        return new Vec(X + v.X, Y + v.Y);
      }

      public Vec Multiply(int r)
      {
        return new Vec(X * r, Y * r);
      }
    }

    public class MyState : IState
    {
      public string Matrix { get; set; }

      public (bool, string message) Fix()
      {
        try
        {
          Matrix = Matrix ?? "abcdefghijklmnopqrstuvwxyz";
          new PasswordMatrix(Matrix);
          return (true, "");
        }
        catch (Exception ex) when (!Helper.IsFatalException(ex))
        {
          return (false, $"Property '{nameof(Matrix)}' is invalid: " + ex.Message);
        }
      }
    }
  }
}
