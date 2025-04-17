using System;
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
        public List<Node> nodes { get; set; }
        public List<Edge> edges { get; set; }
        public Graph() 
        {
            nodes = new List<Node>();
            edges = new List<Edge>();
        }
        int order => nodes.Count; // the number of nodes
        int size => edges.Count; // the number of edges
        bool directed = true;
        bool weighted = true;
        bool cyclic = false;
        bool connected = true;
        int connectionDegree = 1; // the number of independent, non-connected sub-graphs
        public void AddNode(int id, string name, double weight)
        {
            if (nodes.Any(n => n.id == id))
            {
                Console.WriteLine("Node with this ID already exists.");
                return;
            }
            nodes.Add(new Node(id, name, weight));
            UpdateGraphProperties();
        }
        public void RemoveNode(int id)
        {
            Node? node = nodes.FirstOrDefault(n => n.id == id);
            if (node == null)
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
            nodes.Remove(node);
            UpdateGraphProperties();
        }
        public void AddEdge(int id, int fromId, int toId, double weight)
        {
            Node? fromNode = nodes.FirstOrDefault(n => n.id == fromId);
            Node? toNode = nodes.FirstOrDefault(n => n.id == toId);
            if (fromNode == null || toNode == null)
            {
                Console.WriteLine("One or both nodes do not exist.");
                return;
            }
            Edge edge = new Edge(id, fromNode, toNode, weight);
            fromNode.edgesOut.Add(edge);
            toNode.edgesIn.Add(edge);
            edges.Add(edge);
            UpdateGraphProperties();
        }
        public void RemoveEdge(int id)
        {
            Edge? edge = edges.FirstOrDefault(n => n.id == id);
            if (edge == null)
            {
                return;
            }
            if (edge.from != null)
            {
                edge.from.edgesOut.Remove(edge);
            }
            if (edge.to != null)
            {
                edge.to.edgesIn.Remove(edge);
            }
            edges.Remove(edge);
            UpdateGraphProperties();
        }
        // Helper classes for saving and loading a graph using json
        class GraphData
        {
            public List<NodeData> nodes { get; set; } = new();
            public List<EdgeData> edges { get; set; } = new();
            public bool directed { get; set; }
            public bool weighted { get; set; }
            public bool cyclic { get; set; }
            public bool connected { get; set; }
            public int connectionDegree { get; set; }
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
            var options = new JsonSerializerOptions { WriteIndented = true };
            var data = new GraphData
            {
                nodes = nodes.Select(n => new NodeData { id = n.id, name = n.name, weight = n.weight }).ToList(),
                edges = edges.Select(e => new EdgeData { id = e.id, fromId = e.from.id, toId = e.to.id, weight = e.weight }).ToList(),
                directed = directed,
                weighted = weighted,
                cyclic = cyclic,
                connected = connected,
                connectionDegree = connectionDegree
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
            var nodeDict = new Dictionary<int, Node>();
            foreach (var nodeData in data.nodes)
            {
                var node = new Node(nodeData.id, nodeData.name, nodeData.weight);
                nodes.Add(node);
                nodeDict[node.id] = node;
            }
            foreach (var edgeData in data.edges)
            {
                if (nodeDict.TryGetValue(edgeData.fromId, out Node? fromNode) &&
                    nodeDict.TryGetValue(edgeData.toId, out Node? toNode))
                {
                    Edge edge = new Edge(edgeData.id, fromNode, toNode, edgeData.weight);
                    fromNode.edgesOut.Add(edge);
                    toNode.edgesIn.Add(edge);
                    edges.Add(edge);
                }
            }
            directed = data.directed;
            weighted = data.weighted;
            cyclic = data.cyclic;
            connected = data.connected;
            connectionDegree = data.connectionDegree;
        }
        public void UpdateGraphProperties()
        {
            connected = CheckConnectivity();
            cyclic = CheckCycles();
            connectionDegree = CountConnectedComponents();
        }
        public bool CheckConnectivity()
        {
            // To-be implemented
            return true;
        }
        public bool CheckCycles()
        {
            // To-be implemented
            return false;
        }
        public int CountConnectedComponents()
        {
            // To-be implemented
            return 1;
        }
        public void CheckDirectionality()
        {
            directed = true;
            foreach (Edge edge in edges)
            {
                directed = true;
                foreach (Edge secondEdge in edges)
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
            Console.WriteLine($"Cyclic: {cyclic}");
            Console.WriteLine($"Connected: {connected}");
            Console.WriteLine($"Connection Degree: {connectionDegree}");
            Console.WriteLine();

            Console.WriteLine("--- Nodes ---");
            foreach (var node in nodes)
            {
                Console.WriteLine($"ID: {node.id}, Name: {node.name}, Weight: {node.weight}, Degree: {node.degree} (In: {node.degreeIn}, Out: {node.degreeOut})");
            }

            Console.WriteLine();

            Console.WriteLine("--- Edges ---");
            foreach (var edge in edges)
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
                nodes.Add(new Node(i + 1, nodeName, nodeWeight));
            }
            Console.Write("Enter the number of edges: ");
            int edgeCount = int.Parse(Console.ReadLine());
            for (int i = 0; i < edgeCount; i++)
            {
                Console.Write($"Enter ID of the from node for Edge {i + 1}: ");
                int fromId = int.Parse(Console.ReadLine());
                Node fromNode = nodes.FirstOrDefault(n => n.id == fromId);
                if (fromNode == null)
                {
                    Console.WriteLine("Invalid node ID. Try again.");
                    i--;
                    continue;
                }
                Console.Write($"Enter ID of the to node for Edge {i + 1}: ");
                int toId = int.Parse(Console.ReadLine());
                Node toNode = nodes.FirstOrDefault(n => n.id == toId);
                if (toNode == null)
                {
                    Console.WriteLine("Invalid node ID. Try again.");
                    i--;
                    continue;
                }
                double edgeWeight = 1;
                if (weighted)
                {
                    Console.Write($"Enter weight for Edge {i + 1}: ");
                    edgeWeight = double.Parse(Console.ReadLine());
                }
                edges.Add(new Edge(i + 1, fromNode, toNode, edgeWeight));
                fromNode.edgesOut.Add(edges.Last());
                toNode.edgesIn.Add(edges.Last());
            }
            Console.WriteLine("Graph created successfully!");
        }
    }
    class Execute
    {
        public static void Main(string[] args)
        {
            Graph newgraph = new Graph();
            newgraph.InputGraph();
            newgraph.CheckDirectionality();
            newgraph.PrintGraph();
        }
    }
}