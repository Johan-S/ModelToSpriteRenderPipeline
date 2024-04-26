using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PositionRect {
   public int dimensions;
   public Vector2Int size;
   public IEnumerable<Position> positions {
      get {
         for (int d = 0; d < dimensions; ++d) {
            for (int y = 0; y < size.y; ++y) {
               for (int x = 0; x < size.x; ++x) {
                  yield return new Position(new Vector2Int(x, y), d);
               }
            }
         }
      }
   }
   public IEnumerable<(int, Position)> position_ids {
      get {
         int i = 0;
         for (int d = 0; d < dimensions; ++d) {
            for (int y = 0; y < size.y; ++y) {
               for (int x = 0; x < size.x; ++x) {
                  yield return (i++, new Position(new Vector2Int(x, y), d));
               }
            }
         }
      }
   }

}

[System.Serializable]
public struct Position {

   public IEnumerable<Position> as_iter {
      get {
         yield return this;
      }
   }

   public Position(int x, int y, int a = 0) {
      this.coordinates = new Vector2Int(x, y);
      this.dimension = a;
   }

   public Position(Vector2Int coordinates, int dimension) {
      this.coordinates = coordinates;
      this.dimension = dimension;
   }

   public Position((int, int) coordinates, int dimension) {
      this.coordinates = new Vector2Int(coordinates.Item1, coordinates.Item2);
      this.dimension = dimension;
   }

   public Position(((int, int), int) a) {
      this.coordinates = new Vector2Int(a.Item1.Item1, a.Item1.Item2);
      this.dimension = a.Item2;
   }

   public Position((Vector2Int, int) a) {
      this.coordinates = a.Item1;
      this.dimension = a.Item2;
   }

   public Position DimensionalNeighbour() {
      var res = this;
      res.dimension ^= 1;
      return res;
   }

   public IEnumerable<Position> HexPosWithin(int radius) {
      foreach (var p in coordinates.HexPosWithin(radius)) {
         yield return new Position(p, dimension);
      }
   }

   public int HexDist(Position o) {
      if (dimension != o.dimension) return 999999;
      return coordinates.HexDist(o.coordinates);
   }

   public static bool operator ==(Position a, Position b) {
      return a.coordinates == b.coordinates && a.dimension == b.dimension;
   }
   public static bool operator !=(Position a, Position b) {
      return !(a == b);
   }

   public static Position operator +(Position a, Position p) {
      a.coordinates += p.coordinates;
      a.dimension += p.dimension;
      return a;
   }
   public static Position operator +(Position a, Vector2Int p) {
      a.coordinates += p;
      return a;
   }

   public static Position operator +(Vector2Int p, Position a) {
      a.coordinates += p;
      return a;
   }
   public static Position operator +(Position a, int2 p) {
      a.coordinates += p;
      return a;
   }

   public static Position operator +(int2 p, Position a) {
      a.coordinates += p;
      return a;
   }
   public static Position operator -(Position a, Vector2Int p) {
      a.coordinates = a.coordinates - p;
      return a;
   }

   public static Position operator -(Vector2Int p, Position a) {
      a.coordinates = p - a.coordinates;
      return a;
   }
   public Vector2Int vec => coordinates;
   public int2 int2 => new int2(x, y);

   [Data(id = 1)]
   public Vector2Int coordinates;
   [Data(id = 2)]
   public int dimension;
   public Vector2Int c => coordinates;

   public Position RandomNeighbor() {
      return (Hexagon.RandomNeighbor(coordinates), dimension);
   }

   public override bool Equals(object obj) {
      return obj is Position position &&
             coordinates.x == position.coordinates.x && coordinates.y == position.coordinates.y &&
             dimension == position.dimension;
   }

   public override int GetHashCode() {
      int hashCode = -1812277675;
      hashCode = hashCode * -1521134295 + coordinates.GetHashCode();
      hashCode = hashCode * -1521134295 + dimension.GetHashCode();
      return hashCode;
   }

   public ref T Lookup<T>(T[,,] x) {
      return ref x[dimension, coordinates.x, coordinates.y];
   }

   public int x => coordinates.x;
   public int y => coordinates.y;

   public static implicit operator Position(((int, int), int) a) => new Position(a);
   public static implicit operator Position((Vector2Int, int) a) => new Position(a);

   public static implicit operator Position((int, int) a) => new Position(a, 0);
   public static implicit operator Position((int, int, int) a) => new Position(a.Item1, a.Item2, a.Item3);

   public static implicit operator Position(int2 a) => new Position(a.x, a.y, 0);

   public override string ToString() {
      return $"Pos({x}, {y}: {dimension})";
   }


   public Position North() {
      return new Position (coordinates.North(), dimension);
   }
   public Position South() {
      return new Position(coordinates.South(), dimension);
   }
   public Position NorthEast() {
      return new Position(coordinates.NorthEast(), dimension);
   }
   public Position NorthWest() {
      return new Position(coordinates.NorthWest(), dimension);
   }
   public Position SouthEast() {
      return new Position(coordinates.SouthEast(), dimension);
   }
   public Position SouthWest() {
      return new Position(coordinates.SouthWest(), dimension);
   }
}

[System.Serializable]
public struct FPosition {

   public IEnumerable<FPosition> as_iter {
      get {
         yield return this;
      }
   }

   public bool IsInteger() {
      return Equals(Round());
   }

   public FPosition Round() {
      return new FPosition(Mathf.Round(x), Mathf.Round(y), dimension);
   }
   public Position RoundToInt() {
      return new Position(Mathf.RoundToInt(x), Mathf.RoundToInt(y), dimension);
   }

   public FPosition(Position pos) {
      this.coordinates = new Vector2(pos.x, pos.y);
      this.dimension = pos.dimension;
   }

   public FPosition(float x, float y, int a = 0) {
      this.coordinates = new Vector2(x, y);
      this.dimension = a;
   }

   public FPosition(Vector2 coordinates, int dimension) {
      this.coordinates = coordinates;
      this.dimension = dimension;
   }

   public FPosition((float, float) coordinates, int dimension) {
      this.coordinates = new Vector2(coordinates.Item1, coordinates.Item2);
      this.dimension = dimension;
   }

   public FPosition(((float, float), int) a) {
      this.coordinates = new Vector2(a.Item1.Item1, a.Item1.Item2);
      this.dimension = a.Item2;
   }

   public FPosition((Vector2, int) a) {
      this.coordinates = a.Item1;
      this.dimension = a.Item2;
   }

   public static bool operator ==(FPosition a, FPosition b) {
      return a.coordinates == b.coordinates && a.dimension == b.dimension;
   }
   public static bool operator !=(FPosition a, FPosition b) {
      return !(a == b);
   }

   public static FPosition operator +(FPosition a, FPosition p) {
      a.coordinates += p.coordinates;
      a.dimension += p.dimension;
      return a;
   }
   public static FPosition operator +(FPosition a, Vector2 p) {
      a.coordinates += p;
      return a;
   }

   public static FPosition operator +(Vector2 p, FPosition a) {
      a.coordinates += p;
      return a;
   }
   public static FPosition operator -(FPosition a, Vector2 p) {
      a.coordinates = a.coordinates - p;
      return a;
   }

   public static FPosition operator -(Vector2 p, FPosition a) {
      a.coordinates = p - a.coordinates;
      return a;
   }
   public Vector2 vec => coordinates;

   [Data(id = 1)]
   public Vector2 coordinates;
   [Data(id = 2)]
   public int dimension;
   public Vector2 c => coordinates;

   public override bool Equals(object obj) {
      return obj is FPosition FPosition &&
             coordinates == FPosition.coordinates &&
             dimension == FPosition.dimension;
   }

   public override int GetHashCode() {
      int hashCode = -1812277675;
      hashCode = hashCode * -1521134295 + coordinates.GetHashCode();
      hashCode = hashCode * -1521134295 + dimension.GetHashCode();
      hashCode = hashCode * -1521134295 + c.GetHashCode();
      return hashCode;
   }

   public float x => coordinates.x;
   public float y => coordinates.y;

   public static implicit operator FPosition(((float, float), int) a) => new FPosition(a);
   public static implicit operator FPosition((Vector2, int) a) => new FPosition(a);

   public static implicit operator FPosition((float, float) a) => new FPosition(a, 0);
   public static implicit operator FPosition((float, float, int) a) => new FPosition(a.Item1, a.Item2, a.Item3);


   public static implicit operator FPosition(Position pos) => new FPosition(pos);

   public static implicit operator FPosition(int2 a) => new FPosition(a.x, a.y, 0);
   public static implicit operator FPosition(float2 a) => new FPosition(a.x, a.y, 0);

   public override string ToString() {
      return $"FPos({x}, {y}: {dimension})";
   }
}