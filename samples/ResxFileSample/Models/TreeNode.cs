using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ResxFileSample.Models
{
    public class TreeNode
    {
        public string name { get; set; }

        public List<TreeNode> children { get; set; }
    }
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
