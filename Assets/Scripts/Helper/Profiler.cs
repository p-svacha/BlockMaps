using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.Profiling
{
    public static class Profiler
    {
        public static int Depth = 0;
        public static StopwatchTree Tree = new StopwatchTree("Root", 0, null);
        public static StopwatchTree ActiveLayer = Tree;


        public static void Begin(string name)
        {
            Depth++;
            StopwatchTree newTree = new StopwatchTree(name, Depth, parent: ActiveLayer);
            ActiveLayer.Children.Add(newTree);
            ActiveLayer = newTree;
        }
        public static void End(string name)
        {
            if (ActiveLayer.Name != name) throw new System.Exception($"Can't stop stopwatch {name} because {ActiveLayer.Name} is currently running and needs to be stopped first.");

            ActiveLayer.Stop();
            ActiveLayer = ActiveLayer.Parent;
            Depth--;
        }
        public static void Reset()
        {
            Depth = 0;
            Tree = new StopwatchTree("Root", 0, null);
            ActiveLayer = Tree;
        }


        public static void LogAndClearResults()
        {
            Debug.Log($"[Profiler Results] {LogTree(Tree, logRoot: false)}");
            Reset();
        }
        private static string LogTree(StopwatchTree tree, bool logRoot)
        {
            string log = "";

            if (logRoot)
            {
                string prefix = "";
                for (int i = 0; i < tree.Depth; i++) prefix += "\t";
                log += $"{prefix}{tree.Name}: {tree.Stopwatch.ElapsedMilliseconds} ms";
                long selfTime = tree.Stopwatch.ElapsedMilliseconds;
                foreach (StopwatchTree child in tree.Children) selfTime -= child.Stopwatch.ElapsedMilliseconds;
                log += $" (self = {selfTime} ms)";
            }

            foreach (StopwatchTree child in tree.Children) log += $"\n{LogTree(child, logRoot: true)}";
            return log;
        }
    }

    public class StopwatchTree
    {
        public string Name;
        public int Depth;
        public StopwatchTree Parent;
        public List<StopwatchTree> Children;
        public System.Diagnostics.Stopwatch Stopwatch;

        public StopwatchTree(string name, int depth, StopwatchTree parent)
        {
            Name = name;
            Depth = depth;
            Parent = parent;
            Children = new List<StopwatchTree>();
            Stopwatch = new System.Diagnostics.Stopwatch();
            Stopwatch.Start();
        }

        public void Stop()
        {
            Stopwatch.Stop();
        }
    }
}
