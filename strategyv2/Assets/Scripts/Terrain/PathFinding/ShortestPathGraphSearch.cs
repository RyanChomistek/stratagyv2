using UnityEngine;
using System;
using System.Collections.Generic;
using Priority_Queue;
/// <summary>
/// Based on uniform-cost-search/A* from the book
/// Artificial Intelligence: A Modern Approach 3rd Ed by Russell/Norvig
/// </summary>
public class ShortestPathGraphSearch<State, Action>
{
    class SearchNode<State, Action> : IComparable<SearchNode<State, Action>>
    {
        public SearchNode<State, Action> parent;
        public State state;
        public Action action;
        public float g; // cost
        public float f; // estimate
        public SearchNode(SearchNode<State, Action> parent, float g, float f, State state, Action action)
        {
            this.parent = parent;
            this.g = g;
            this.f = f;
            this.state = state;
            this.action = action;
        }
        // Reverse sort order (smallest numbers first)
        public int CompareTo(SearchNode<State, Action> other)
        {
            return other.f.CompareTo(f);
        }
        public override string ToString()
        {
            return "SN {f:" + f + ", state: " + state + " action: " + action + "}";
        }
    }
    private IShortestPath<State, Action> info;
    public ShortestPathGraphSearch(IShortestPath<State, Action> info)
    {
        this.info = info;
    }
    
    public List<Action> GetShortestPath(State fromState, State toState)
    {
        SimplePriorityQueue< SearchNode < State, Action > ,float > frontier = new SimplePriorityQueue<SearchNode<State, Action>, float>();
        HashSet<State> exploredSet = new HashSet<State>();
        Dictionary<State, SearchNode<State, Action>> frontierMap = new Dictionary<State, SearchNode<State, Action>>();
        SearchNode<State, Action> startNode = new SearchNode<State, Action>(null, 0, 0, fromState, default(Action));
        frontier.Enqueue(startNode, 0);
        frontierMap.Add(fromState, startNode);
        while (true)
        {
            if (frontier.Count == 0) return null;
            SearchNode<State, Action> node = frontier.Dequeue();
            if (node.state.Equals(toState)) return BuildSolution(node);
            exploredSet.Add(node.state);
            // expand node and add to frontier
            foreach (Action action in info.Expand(node.state))
            {
                State child = info.ApplyAction(node.state, action);
                SearchNode<State, Action> frontierNode = null;
                bool isNodeInFrontier = frontierMap.TryGetValue(child, out frontierNode);
                if (!exploredSet.Contains(child) && !isNodeInFrontier)
                {
                    SearchNode<State, Action> searchNode = CreateSearchNode(node, action, child, toState);
                    frontier.Enqueue(searchNode, searchNode.f);
                    exploredSet.Add(child);
                }
                else if (isNodeInFrontier)
                {
                    SearchNode<State, Action> searchNode = CreateSearchNode(node, action, child, toState);
                    if (frontierNode.f > searchNode.f)
                    {
                        //frontier.Replace(frontierNode, frontierNode.f, searchNode.f);
                        frontier.UpdatePriority(frontierNode, searchNode.f);
                    }
                }
            }
        }
    }
    
    private SearchNode<State, Action> CreateSearchNode(SearchNode<State, Action> node, Action action, State child, State toState)
    {
        float cost = info.ActualCost(node.state, action);
        float heuristic = info.Heuristic(child, toState);
        return new SearchNode<State, Action>(node, node.g + cost, node.g + cost + heuristic, child, action);
    }
    private List<Action> BuildSolution(SearchNode<State, Action> seachNode)
    {
        List<Action> list = new List<Action>();
        while (seachNode != null)
        {
            if ((seachNode.action != null) && (!seachNode.action.Equals(default(Action))))
            {
                list.Insert(0, seachNode.action);
            }
            seachNode = seachNode.parent;
        }
        return list;
    }
}