using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Structures
{
    [Serializable]
    public class Node<T>
    {
        public T Value {get; set;}
        public List<Node<T>> Children {get; set;}

        public Node(T data, Node<T> parent = null) {
            Value = data;
            Parent = parent;
            Children = new List<Node<T>>();
        }

        public Node(){}

        public Node<T> this[int idx] => Children[idx];

        [JsonIgnore]
        [IgnoreDataMember]
        public List<Node<T>> Siblings {get {
            if(IsRoot) return new List<Node<T>>();
            return Parent.Children.Where(x => !x.Equals(this)).ToList();
        }}

        [JsonIgnore]
        [IgnoreDataMember]
        public bool IsRoot => Parent == null;
        
        [JsonIgnore]
        [IgnoreDataMember]
        public bool IsLeaf => Children.Count == 0;

        //Navigation
        [JsonIgnore]
        [IgnoreDataMember]
        public Node<T> Parent {get; private set;}

        [JsonIgnore]
        [IgnoreDataMember]
        public int Depth => IsLeaf ? 0 : 1 + Children.Max(x => x.Depth);

        [JsonIgnore]
        [IgnoreDataMember]
        public int Count => 1 + Children.Sum(x => x.Count);

        [JsonIgnore]
        [IgnoreDataMember]
        public int ChildCount => Count - 1;

        [JsonIgnore]
        [IgnoreDataMember]
        public int SiblingCount => IsRoot ? 0 : Parent.ChildCount - 1;

        [JsonIgnore]
        [IgnoreDataMember]
        public Node<T> Root 
            => IsRoot ? this : Parent.Root;

        //Functions
        public Node<T> Add(T data) {
            var node = new Node<T>(data, this);
            Children.Add(node);
            return node;
        }

        public void AddRange(params T[] values) {
            foreach (var v in values)
                Children.Add(new Node<T>(v, this));
        }

        public void RemoveAt(int index) => Children.RemoveAt(index);
        public void Remove(Node<T> elem) => Children.Remove(elem);
        public void Clear() => Children.Clear();

        public void DeepClear() {
            if(IsLeaf) return;
            foreach (var child in Children)
                child.DeepClear();
            Clear();
        }

        //Visiting (preorder?)
        public void Visit(Action<T> action) {
            action(Value);
            Children.ForEach(x => x.Visit(action));
        }

        //Visit preorder
        //Visit inorder
        //Visit postorder

        //Linking and cloning
        public void Link(Node<T> otherTree) {
            otherTree.Parent = this;
            Children.Add(otherTree);
        }

        public void DeepLink(Node<T> otherTree) {
            //Deep clone using serialization :D
            otherTree = otherTree.DeepClone();
            LinkChildren(otherTree);
            Link(otherTree);
        }

        public Node<T> DeepClone()
        {
            using var ms = new System.IO.MemoryStream();

            var formatter = new BinaryFormatter();
            formatter.Serialize(ms, this);
            ms.Position = 0;

            return (Node<T>)formatter.Deserialize(ms);
        }

        private void LinkChildren(Node<T> node) {
            if(node.IsLeaf) return;
            foreach (var c in node.Children) {
                c.Parent = node;
                LinkChildren(c);
            }
        }
        
        public override string ToString()
            => JsonSerializer.Serialize(this);

        //Comparison
        public override bool Equals(object obj)
        {
            if(obj.GetType() != this.GetType()) return false;
            return this.GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
            => base.GetHashCode() ^ Value.GetHashCode();

    }
}