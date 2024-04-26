
   using System.Collections;
   using System.Collections.Generic;
   using UnityEngine;

   [System.Serializable]
   public class TileLayout<T> : IEnumerable<T> {


      public TileLayout() {

      }

      public TileLayout(int dimensions, Vector2Int size) {
         this.dimensions = dimensions;
         this.size = size;
         tiles = new T[dimensions * size.x * size.y];
      }
      public TileLayout(Position dim) {
         this.dimensions = dim.dimension;
         this.size = dim.coordinates;
         tiles = new T[dimensions * size.x * size.y];
      }

      [Data(id = 1)]
      public int dimensions;
      [Data(id = 2)]
      public Vector2Int size;
      [Data(id = 3)]
      public T[] tiles;

      public PositionRect map_size => new PositionRect { dimensions = dimensions, size = size };

      public Position dim => new Position(size, dimensions);

      public void SetAll(T val) {
         for (int i = 0; i < tiles.Length; ++i) {
            tiles[i] = val;
         }
      }

      public bool IsValid(Position p) {
         if (p.dimension < 0 || p.dimension >= dimensions) {
            return false;
         }
         if (p.x < 0 || p.x >= size.x) {
            return false;
         }
         if (p.y < 0 || p.y >= size.y) {
            return false;
         }
         return true;
      }

      public IEnumerable<Position> ValidPositions(System.Predicate<T> f) {
         for (int i = 0; i < tiles.Length; ++i) {
            if (f(tiles[i])) {
               yield return PositionOf(i);
            }
         }
      }

      public TileLayout<U> Map<U>(System.Func<T, U> f) {
         TileLayout<U> res = new TileLayout<U>(dimensions, size);
         for (int i = 0; i < tiles.Length; ++i) {
            res.tiles[i] = f(tiles[i]);
         }
         return res;
      }

      public TileLayout<T> Copy() {
         var r = new TileLayout<T>(dimensions, size);

         tiles.CopyTo(r.tiles, 0);
         return r;
      }

      public int IndexOf(Position p) {
         return p.x + size.x * (p.y + size.y * (p.dimension));
      }
      public Position PositionOf(int index) {
         int x = index % size.x;
         index /= size.x;
         int y = index % size.y;
         index /= size.y;
         int dim = index;
         return new Position(new Vector2Int(x, y), dim);
      }
      public IEnumerable<(int, Position)> position_ids {
         get {
            return map_size.position_ids;
         }
      }

      public IEnumerable<Position> positions {
         get {
            return map_size.positions;
         }
      }
      private IEnumerable<T> getenum() {
         return tiles;
      }

      public IEnumerator<T> GetEnumerator() {
         return getenum().GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator() {
         return tiles.GetEnumerator();
      }

      public T this[Position p] {
         get {
            Debug.Assert(IsValid(p), p);
            return tiles[IndexOf(p)];
         }
         set {
            Debug.Assert(IsValid(p), p);
            tiles[IndexOf(p)] = value;
         }
      }

      public T this[int p] {
         get {
            return tiles[p];
         }
         set {
            tiles[p] = value;
         }
      }

      public static implicit operator bool(TileLayout<T> t) {
         return t != null;
      }
   }