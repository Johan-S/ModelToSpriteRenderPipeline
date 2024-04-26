using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

public class GameStateTracker {

   public GameStateTracker(IGameStateSource source) {
      state = source.state - 1;
      this.source = source;
   }

   int state;
   IGameStateSource source;

   public IGameStateSource Source() => source;

   public bool IsObsolete() {
      return source.obsolete;
   }


   public bool IsDirty() {
      return source.state != state;

   }
   public void SetDirty() {
      state = source.state - 1;

   }
   public bool IsCurrent() {
      return source.state == state;

   }
   public bool SyncDirty() {
      bool dirty = IsDirty();
      if (dirty) {
         Sync();
      }
      return dirty;
   }

   public GameStateTracker Sync() {
      state = source.state;
      return this;
   }
}

public interface IGameStateSource {

   public bool obsolete {
      get;
   }
   public int state {
      get;
   }

}

public class GameStateSource : IGameStateSource {
   public int state { get; set; } = 1;

   public bool obsolete {
      get; set;
   }

   public float last_updated;

   public void MarkObsolete() {
      obsolete = true;
   }

   public void MarkDirty() {
      last_updated = Time.time;
      state++;
   }
}

public static class GameStateTrackerContainer_Overloads {

   public static GameStateTracker Track(this IGameStateSource source) {
      return new GameStateTracker(source);
   }
}


public class GameStateTrackerContainer {

   public GameStateTrackerContainer() {
   }

   List<GameStateTracker> to_track = new List<GameStateTracker>();

   public void Add(GameStateSource source) {
      to_track.Add(source.Track());
   }

   public void Add(GameStateTracker tr) {
      TrackTarget(tr.Source());
   }

   public void TrackTarget(IGameStateSource source) {
      to_track.Add(source.Track());
   }

   public void TrackTarget(GameStateTracker tr) {
      TrackTarget(tr.Source());
   }

   bool FilterObsolete() {
      int n_in = to_track.Count;
      to_track.Filter(x => !x.IsObsolete());
      return n_in != to_track.Count;
   }

   public bool IsDirty() {
      return FilterObsolete() || to_track.Exists(x => x.IsDirty());

   }
   public bool IsCurrent() {
      return !IsDirty();

   }
   public bool SyncDirty() {
      bool dirty = IsDirty();
      if (dirty) {
         Sync();
      }
      return dirty;
   }

   public void Sync() {
      foreach (var t in to_track) {
         t.Sync();
      }
   }
}
