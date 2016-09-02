using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ResxFileSample.Models
{
    public class TreeNode : IEquatable<TreeNode>
    {
        public string name { get; set; }
        
        public string id { get; set; }

        public bool isParent { get; set; }

        public bool Equals(TreeNode other)
        {
            if (Object.ReferenceEquals(other, null)) return false;
            
            if (Object.ReferenceEquals(this, other)) return true;
            
            return id.Equals(other.id);
        }
        
        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
    }

    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
