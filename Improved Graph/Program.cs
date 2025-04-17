using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Programme
{
    class Node
    {
        public static int lastId = -1;
        public int id { get; set; }
        public string name { get; set; }
        public double weight { get; set; }
        public List<Edge> edgesIn { get; set; } = new();
        public List<Edge> edgesOut { get; set; } = new();
        public int degreeIn => edgesIn.Count;
        public int degreeOut => edgesOut.Count;
        public int degree => degreeIn + degreeOut;
        public Node() { }
        public Node(int id, string name, double weight)
        {
            this.id = id;
            this.name = name;
            this.weight = weight;
        }
    }
    class Edge
    {
        public static int lastId = -1;
        public int id { get; set; }
        public Node? from { get; set; }
        public Node? to { get; set; }
        public double weight { get; set; }
        public Edge() { }
        public Edge(int id, Node from, Node to, double weight)
        {
            this.id = id;
            this.from = from;
            this.to = to;
            this.weight = weight;
        }
    }
    class Graph
    {
        public Dictionary<int, Node> nodes { get; set; }
        public Dictionary<int, Edge> edges { get; set; }
        public Graph() 
        {
            nodes = new Dictionary<int, Node>();
            edges = new Dictionary<int, Edge>();
        }
        int order => nodes.Count; // the number of nodes
        int size => edges.Count; // the number of edges
        public bool directed = true;
        public bool weighted = true;
        public bool selfLooping = false;
        public bool connected = true;
        public int connectionDegree = 1; // the number of independent, non-connected sub-graphs
        public bool negativeWeights = false; // if the graph has negative weights
        public bool negativeCycles = false; // if the graph has negative cycles
        public void AddNode(int id, string name, double weight)
        {
            if (nodes.ContainsKey(id))
            {
                Console.WriteLine("Node with this ID already exists.");
                return;
            }
            nodes[id] = new Node(id, name, weight);
            UpdateGraphProperties();
        }

        public void RemoveNode(int id)
        {
            if (!nodes.TryGetValue(id, out Node? node))
            {
                return;
            }
            foreach (var edge in node.edgesIn.ToList())
            {
                RemoveEdge(edge.id);
            }
            foreach (var edge in node.edgesOut.ToList())
            {
                RemoveEdge(edge.id);
            }
            nodes.Remove(id);
            UpdateGraphProperties();
        }

        public void AddEdge(int id, int fromId, int toId, double weight)
        {
            if (!nodes.TryGetValue(fromId, out Node? fromNode) || !nodes.TryGetValue(toId, out Node? toNode))
            {
                Console.WriteLine("One or both nodes do not exist.");
                return;
            }

            if (edges.ContainsKey(id))
            {
                Console.WriteLine("Edge with this ID already exists.");
                return;
            }

            Edge edge = new Edge(id, fromNode, toNode, weight);
            fromNode.edgesOut.Add(edge);
            toNode.edgesIn.Add(edge);
            edges[id] = edge;
            UpdateGraphProperties();
        }

        public void RemoveEdge(int id)
        {
            if (!edges.TryGetValue(id, out Edge? edge))
            {
                return;
            }
            edge.from?.edgesOut.Remove(edge);
            edge.to?.edgesIn.Remove(edge);
            edges.Remove(id);
            UpdateGraphProperties();
        }

        // Helper classes for saving and loading a graph using json
        class GraphData
        {
            public List<NodeData> nodes { get; set; } = new();
            public List<EdgeData> edges { get; set; } = new();
            public bool directed { get; set; }
            public bool weighted { get; set; }
            public bool selfLooping { get; set; }
            public bool connected { get; set; }
            public int connectionDegree { get; set; }
            public bool negativeWeights { get; set; }
            public bool negativeCycles { get; set; }
        }
        class NodeData
        {
            public int id { get; set; }
            public string name { get; set; }
            public double weight { get; set; }
        }
        class EdgeData
        {
            public int id { get; set; }
            public int fromId { get; set; }
            public int toId { get; set; }
            public double weight { get; set; }
        }
        public void SaveGraph(string filePath)
        {
            UpdateGraphProperties();
            var options = new JsonSerializerOptions { WriteIndented = true };
            var data = new GraphData
            {
                nodes = nodes.Values.Select(n => new NodeData { id = n.id, name = n.name, weight = n.weight }).ToList(),
                edges = edges.Values.Select(e => new EdgeData { id = e.id, fromId = e.from.id, toId = e.to.id, weight = e.weight }).ToList(),
                directed = directed,
                weighted = weighted,
                selfLooping = selfLooping,
                connected = connected,
                connectionDegree = connectionDegree,
                negativeWeights = negativeWeights,
                negativeCycles = negativeCycles
            };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(filePath, json);
        }
        public void LoadGraph(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found!");
                return;
            }
            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<GraphData>(json);
            if (data == null)
            {
                return;
            }
            nodes.Clear();
            edges.Clear();
            foreach (var nodeData in data.nodes)
            {
                var node = new Node(nodeData.id, nodeData.name, nodeData.weight);
                nodes[node.id] = node;
            }
            foreach (var edgeData in data.edges)
            {
                if (nodes.TryGetValue(edgeData.fromId, out Node? fromNode) &&
                    nodes.TryGetValue(edgeData.toId, out Node? toNode))
                {
                    Edge edge = new Edge(edgeData.id, fromNode, toNode, edgeData.weight);
                    fromNode.edgesOut.Add(edge);
                    toNode.edgesIn.Add(edge);
                    edges[edge.id] = edge;
                }
            }
            directed = data.directed;
            weighted = data.weighted;
            selfLooping = data.selfLooping;
            connected = data.connected;
            connectionDegree = data.connectionDegree;
            negativeWeights = data.negativeWeights;
            negativeCycles = data.negativeCycles;
        }
        public void UpdateGraphProperties()
        {
            CheckConnectivity();
            selfLooping = CheckSelfLoop();
            negativeCycles = CheckNegativeCycles();
            negativeWeights = CheckNegativeWeights();
            CheckDirectionality();
        }
        public void CheckConnectivity()
        {
            HashSet<int> visited = new HashSet<int>();
            if (nodes.Count == 0)
            {
                connected = true;
                connectionDegree = 1;
                return;
            }
            int components = 0;
            foreach (Node node in nodes.Values)
            {
                if (!visited.Contains(node.id))
                {
                    visited = DFS(node, visited);
                    components++;
                }
            }
            if (components > 1)
            {
                connected = false;
                connectionDegree = components;
            }
            else
            {
                connected = true;
                connectionDegree = 1;
            }
        }
        public HashSet<int> DFS(Node node, HashSet<int> visited)
        {
            Node current = node;
            Stack<Node> stack = new Stack<Node>();
            stack.Push(current);
            visited.Add(current.id);
            while (stack.Count > 0)
            {
                current = stack.Pop();
                foreach (Edge edge in current.edgesOut)
                {
                    if (!visited.Contains(edge.to.id))
                    {
                        stack.Push(edge.to);
                        visited.Add(edge.to.id);
                    }
                }
            }
            return visited;
        }
        public bool CheckNegativeWeights()
        {
            foreach (Edge edge in edges.Values)
            {
                if (edge.weight < 0)
                {
                    return true; // Negative weight detected
                }
            }
            foreach (Node node in nodes.Values)
            {
                if (node.weight < 0)
                {
                    return true; // Negative weight detected
                }
            }
            return false; // No negative weights detected
        }
        public bool CheckNegativeCycles()
        {
            if (nodes.Count == 0) return false;
            foreach (var startNode in nodes.Values)
            {
                Dictionary<int, double> distances = new Dictionary<int, double>();
                foreach (var node in nodes.Values)
                {
                    distances[node.id] = double.MaxValue;
                }
                distances[startNode.id] = 0;
                int n = nodes.Count;
                for (int i = 0; i < n - 1; i++)
                {
                    foreach (var edge in edges.Values)
                    {
                        if (distances[edge.from.id] != double.MaxValue &&
                            distances[edge.from.id] + edge.weight < distances[edge.to.id])
                        {
                            distances[edge.to.id] = distances[edge.from.id] + edge.weight;
                        }
                    }
                }
                foreach (var edge in edges.Values)
                {
                    if (distances[edge.from.id] != double.MaxValue &&
                        distances[edge.from.id] + edge.weight < distances[edge.to.id])
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool CheckSelfLoop()
        {
            foreach (Edge edge in edges.Values)
            {
                if (edge.from == edge.to)
                {
                    return true; // Self-loop detected
                }
            }
            return false;
        }
        public void CheckDirectionality()
        {
            directed = true;
            foreach (Edge edge in edges.Values)
            {
                directed = true;
                foreach (Edge secondEdge in edges.Values)
                {
                    if (edge.from == secondEdge.to &&
                        edge.to == secondEdge.from &&
                        edge.weight == secondEdge.weight)
                    {
                        directed = false;
                        break;
                    }
                }
                if (directed)
                {
                    return;
                }
            }
            return;
        }
        public void PrintGraph()
        {
            Console.WriteLine("=== Graph ===");
            Console.WriteLine($"Order (Nodes): {order}");
            Console.WriteLine($"Size (Edges): {size}");
            Console.WriteLine($"Directed: {directed}");
            Console.WriteLine($"Weighted: {weighted}");
            Console.WriteLine($"Self-looping: {selfLooping}");
            Console.WriteLine($"Connected: {connected}");
            Console.WriteLine($"Connection Degree: {connectionDegree}");
            Console.WriteLine($"Negative Weights: {negativeWeights}");
            Console.WriteLine($"Negative Cycles: {negativeCycles}");
            Console.WriteLine();

            Console.WriteLine("--- Nodes ---");
            foreach (var node in nodes.Values)
            {
                Console.WriteLine($"ID: {node.id}, Name: {node.name}, Weight: {node.weight}, Degree: {node.degree} (In: {node.degreeIn}, Out: {node.degreeOut})");
            }

            Console.WriteLine();

            Console.WriteLine("--- Edges ---");
            foreach (var edge in edges.Values)
            {
                Console.WriteLine($"ID: {edge.id}, From: {edge.from.name} (ID: {edge.from.id}) -> To: {edge.to.name} (ID: {edge.to.id}), Weight: {edge.weight}");
            }

            Console.WriteLine("=============================\n");
        }
        public void InputGraph()
        {
            Console.WriteLine("Would you like to (1) Load an existing graph or (2) Create a new graph? (Enter 1 or 2)");
            string choice = Console.ReadLine();
            if (choice == "1")
            {
                Console.Write("Enter the file path to load the graph: ");
                string filePath = Console.ReadLine();
                if (File.Exists(filePath))
                {
                    LoadGraph(filePath);
                    Console.WriteLine("Graph loaded successfully!");
                }
                else
                {
                    Console.WriteLine("File not found. Creating a new graph instead.");
                    CreateNewGraph();
                }
            }
            else if (choice == "2")
            {
                CreateNewGraph();
                Console.Write("Would you like to save this graph to a file? (yes/no): ");
                string saveChoice = Console.ReadLine().Trim().ToLower();
                if (saveChoice == "yes")
                {
                    Console.Write("Enter the file path to save the graph: ");
                    string filePath = Console.ReadLine();
                    SaveGraph(filePath);
                    Console.WriteLine("Graph saved successfully!");
                }
            }
            else
            {
                Console.WriteLine("Invalid choice. Please enter 1 or 2.");
                InputGraph();
            }
        }
        private void CreateNewGraph()
        {
            Console.Write("Is the graph directed? (yes/no): ");
            directed = Console.ReadLine().Trim().ToLower() == "yes";
            Console.Write("Is the graph weighted? (yes/no): ");
            weighted = Console.ReadLine().Trim().ToLower() == "yes";
            nodes.Clear();
            edges.Clear();
            Console.Write("Enter the number of nodes: ");
            int nodeCount = int.Parse(Console.ReadLine());
            for (int i = 0; i < nodeCount; i++)
            {
                Console.Write($"Enter name for Node {i + 1}: ");
                string nodeName = Console.ReadLine();
                Console.Write($"Enter weight for Node {i + 1}: ");
                double nodeWeight = double.Parse(Console.ReadLine());
                nodes[i + 1] = new Node(i + 1, nodeName, nodeWeight);
            }
            Console.Write("Enter the number of edges: ");
            int edgeCount = int.Parse(Console.ReadLine());
            for (int i = 0; i < edgeCount; i++)
            {
                Console.Write($"Enter ID of the from node for Edge {i + 1}: ");
                int fromId = int.Parse(Console.ReadLine());
                if (!nodes.ContainsKey(fromId))  // Check if from node exists
                {
                    Console.WriteLine("Invalid node ID. Try again.");
                    i--;  // Retry the current edge input
                    continue;
                }
                Console.Write($"Enter ID of the to node for Edge {i + 1}: ");
                int toId = int.Parse(Console.ReadLine());
                if (!nodes.ContainsKey(toId))  // Check if to node exists
                {
                    Console.WriteLine("Invalid node ID. Try again.");
                    i--;  // Retry the current edge input
                    continue;
                }
                double edgeWeight = 1;
                if (weighted)
                {
                    Console.Write($"Enter weight for Edge {i + 1}: ");
                    edgeWeight = double.Parse(Console.ReadLine());
                }
                var fromNode = nodes[fromId];
                var toNode = nodes[toId];
                Edge edge = new Edge(i + 1, fromNode, toNode, edgeWeight);
                edges[i + 1] = edge;  // Adding to the dictionary with edge id as key

                fromNode.edgesOut.Add(edge);  // Add the edge to the from node's outgoing edges
                toNode.edgesIn.Add(edge);    // Add the edge to the to node's incoming edges
            }
            Console.WriteLine("Graph created successfully!");
        }
    }
    class Algorithms
    {
        public static (Dictionary<int, double> distances, Dictionary<int, int?> previous) BellmanFord(Graph graph, Node startingNode, bool nodeWeights)
        {
            if (graph.negativeCycles)
            {
                Console.WriteLine("Graph has negative cycles."); return (new Dictionary<int, double>(), new Dictionary<int, int?>());
            }
            Dictionary<int, double> distances = new Dictionary<int, double>();
            Dictionary<int, int?> previous = new Dictionary<int, int?>();
            foreach (Node node in graph.nodes.Values)
            {
                distances[node.id] = double.MaxValue;
                previous[node.id] = null;
            }
            distances[startingNode.id] = 0;
            if (nodeWeights)
            {
                for (int i = 0; i < graph.nodes.Count - 1; i++)
                {
                    bool updated = false;
                    foreach (Edge edge in graph.edges.Values)
                    {
                        if (distances[edge.from.id] != double.MaxValue && distances[edge.from.id] + edge.weight + edge.from.weight < distances[edge.to.id])
                        {
                            distances[edge.to.id] = distances[edge.from.id] + edge.weight + edge.from.weight;
                            previous[edge.to.id] = edge.from.id;
                            updated = true;
                        }
                    }
                    if (!updated)
                    {
                        break;
                    }
                }
                foreach (Edge edge in graph.edges.Values)
                {
                    if (distances[edge.from.id] != double.MaxValue && distances[edge.from.id] + edge.weight + edge.from.weight < distances[edge.to.id])
                    {
                        Console.WriteLine("Negative cycle detected.");
                    }
                }
            }
            else
            {
                for (int i = 0; i < graph.nodes.Count - 1; i++)
                {
                    bool updated = false;
                    foreach (Edge edge in graph.edges.Values)
                    {
                        if (distances[edge.from.id] != double.MaxValue && distances[edge.from.id] + edge.weight < distances[edge.to.id])
                        {
                            distances[edge.to.id] = distances[edge.from.id] + edge.weight;
                            previous[edge.to.id] = edge.from.id;
                            updated = true;
                        }
                    }
                    if (!updated)
                    {
                        break;
                    }
                }
                foreach (Edge edge in graph.edges.Values)
                {
                    if (distances[edge.from.id] != double.MaxValue && distances[edge.from.id] + edge.weight < distances[edge.to.id])
                    {
                        Console.WriteLine("Negative cycle detected.");
                    }
                }
            }
            return (distances, previous);
        }
        public static (Dictionary<int, double> distances, Dictionary<int, int?> previous) Dijkstra(Graph graph, Node startingNode, bool nodeWeights)
        {
            Dictionary<int, double> distances = new Dictionary<int, double>();
            Dictionary<int, int?> previous = new Dictionary<int, int?>();
            HashSet<int> visited = new HashSet<int>();
            foreach (Node node in graph.nodes.Values)
            {
                distances[node.id] = double.MaxValue;
                previous[node.id] = null;
            }
            distances[startingNode.id] = 0;
            PriorityQueue<Node, double> priorityQueue = new PriorityQueue<Node, double>();
            priorityQueue.Enqueue(startingNode, 0);
            if (nodeWeights)
            {
                while (priorityQueue.Count > 0)
                {
                    Node currentNode = priorityQueue.Dequeue();
                    if (visited.Contains(currentNode.id))
                    {
                        continue;
                    }
                    visited.Add(currentNode.id);
                    foreach (Edge edge in currentNode.edgesOut)
                    {
                        if (!visited.Contains(edge.to.id) && distances[currentNode.id] + edge.weight + edge.from.weight < distances[edge.to.id])
                        {
                            distances[edge.to.id] = distances[currentNode.id] + edge.weight + edge.from.weight;
                            previous[edge.to.id] = currentNode.id;
                            priorityQueue.Enqueue(edge.to, distances[edge.to.id]);
                        }
                    }
                }
            }
            else
            {
                while (priorityQueue.Count > 0)
                {
                    Node currentNode = priorityQueue.Dequeue();
                    if (visited.Contains(currentNode.id))
                    {
                        continue;
                    }
                    visited.Add(currentNode.id);
                    foreach (Edge edge in currentNode.edgesOut)
                    {
                        if (!visited.Contains(edge.to.id) && distances[currentNode.id] + edge.weight < distances[edge.to.id])
                        {
                            distances[edge.to.id] = distances[currentNode.id] + edge.weight;
                            previous[edge.to.id] = currentNode.id;
                            priorityQueue.Enqueue(edge.to, distances[edge.to.id]);
                        }
                    }
                }
            }
            return (distances, previous);
        }
        public static string ReconstructPath(Dictionary<int, Node> nodes, Dictionary<int, int?> previous, Node targetNode)
        {
            List<string> path = new List<string>();
            int? currentId = targetNode.id;
            while (currentId != null)
            {
                path.Add(nodes[currentId.Value].name);
                currentId = previous[currentId.Value];
            }
            path.Reverse();
            return string.Join(" -> ", path);
        }
    }
    class Test
    {
        public static void TestMethod()
        {
            string filePath = "F:\\graph-project-test1.txt";
            Graph newgraph = new Graph();
            newgraph.LoadGraph(filePath);
            newgraph.PrintGraph();
            (Dictionary<int, double> distances, Dictionary<int, int?> previous) = Algorithms.BellmanFord(newgraph, newgraph.nodes[1], false);
            Console.WriteLine("Distances from starting node:");
            foreach (var kvp in distances)
            {
                Console.WriteLine($"Node {newgraph.nodes[kvp.Key].name}: distances: {kvp.Value}: Path: {Algorithms.ReconstructPath(newgraph.nodes, previous, newgraph.nodes[kvp.Key])}");
            }
        }
    }
    class Execute
    {
        public static void Main(string[] args)
        {
            Test.TestMethod();
        }
    }
}