using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// The Parser creates a data structure of all the Tokens stored within the Lexer (Tokenizer)
/// </summary>
/// <remarks>
/// The data structure is known as an Abstract Syntax Tree and allows objects to be constructed correctly
/// </remarks>

namespace Visave
{
    public static class Parser
    {
        #region Parser Tree
        public sealed class Tree
        {
            public Tree() { }
            public Node root;
        }
        public class Node
        {
            private Lexer.Token m_token;
            private Node m_parent;
            private List<Node> m_children = new();

            public Node(Lexer.Token token) { m_token = token; }
            public Node() {}

            public Lexer.Token GetToken() { return m_token; }
            public Node GetParent() { return m_parent; }
            public List<Node> GetChildren() { return m_children; }

            public void AddChild(Node child)
            {
                child.m_parent = this;
                m_children.Add(child);
            }
            public Node GetLastChild() { return m_children.Count == 0 ? null : m_children[m_children.Count - 1]; }
        }
        private static bool IsEmpty() { return sm_tree == null; }
        #endregion

        // ========================================================================================================================= //

        #region Members
        private static Tree sm_tree = new();
        public static Tree GetTree() { return sm_tree; }
        #endregion

        // ========================================================================================================================= //

        #region Methods
        public static void LoopTree(Node node, int depth)
        {
            foreach (Node child in node.GetChildren())
            {
                UnityEngine.Debug.Log("Node: " + child.GetToken().m_value + " : " + depth + " | Child size: " + child.GetChildren().Count);
                LoopTree(child, depth + 1);
            }
        }
        public static void CreateAST()
        {
            // Initialise tree
            sm_tree.root = new Node(new Lexer.Token(Lexer.Token.Type.OBJECT, "Tree Root"));

            // Create tree from token list (Lexer)
            Lexer.Token curToken;
            bool isArray = false, isComponent = false;
            Node currentNode = new();

            // Loop all tokens
            while (!Lexer.IsEnd())
            {
                // Advance token
                curToken = Lexer.Advance();

                // Is token start of new object
                if (curToken.m_type == Lexer.Token.Type.OBJECT)
                {
                    // Add token to tree root (Start of new object "SaveInstance")
                    sm_tree.root.AddChild(new Node(curToken));
                    curToken = Lexer.Advance();
                }
                // Is token the start of a new component
                if (curToken.m_type == Lexer.Token.Type.COMPONENT)
                {
                    currentNode = new Node(curToken); // Token for the Property Field
                    isComponent = true;
                }
                // Is token an array element
                if (isArray)
                {
                    // Is current token end of array
                    if (curToken.m_type == Lexer.Token.Type.END)
                    {
                        // End of array
                        sm_tree.root.GetLastChild().AddChild(currentNode);
                        isArray = false;
                        isComponent = false;
                        currentNode = new();
                    } else if (curToken.m_type == Lexer.Token.Type.PROPERTY)
                    {
                        // Array element
                        Node fieldNode = new Node(curToken); // Token for the Property Field
                        curToken = Lexer.Advance();
                        fieldNode.AddChild(new Node(curToken));   // Token for the Property Value
                        currentNode.GetLastChild().AddChild(fieldNode);
                    }
                }
                // Is current token a property
                if (curToken.m_type == Lexer.Token.Type.PROPERTY)
                {
                    if (!isComponent)
                    {
                        Node fieldNode = new Node(curToken); // Token for the Property Field
                        // Is property a single variable or group
                        if (Lexer.Peak().m_type == Lexer.Token.Type.VALUE)
                        {
                            curToken = Lexer.Advance();
                            fieldNode.AddChild(new Node(curToken)); // Token for the Property Value
                            sm_tree.root.GetLastChild().AddChild(fieldNode);
                        }
                    }
                    else
                    {
                        Node fieldNode = new Node(curToken); // Token for the Property Field
                        // Is property a single variable or group
                        if (Lexer.Peak().m_type == Lexer.Token.Type.VALUE)
                        {
                            curToken = Lexer.Advance();
                            fieldNode.AddChild(new Node(curToken)); // Token for the Property Value
                            currentNode.AddChild(fieldNode);
                        }
                        // Is current token group
                        if (Lexer.Peak().m_type == Lexer.Token.Type.GROUP)
                        {
                            fieldNode = new Node(curToken); // Token for the Property Field
                            currentNode.AddChild(fieldNode);
                            isArray = true;
                        }
                    }
                }
            }
        }
        #endregion
        // ========================================================================================================================= //
    }
}