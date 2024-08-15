using System;

namespace FlowGraph.Node
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false,Inherited =true)]
    public class NodeNoteAttribute : System.Attribute
    {
        public string note;
        public string packageType;

        public NodeNoteAttribute(string _note = "", string _packageType = "UnityBase")
        {
            note = _note;
            packageType = _packageType;
        }
    }
}
