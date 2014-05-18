using System.Collections.Generic;


namespace UOS
{
    public class TreeNode
    {
        private List<TreeNode> parent;

        public List<TreeNode> children { get; private set; }
        public UpDriver driver { get; private set; }


        public TreeNode(UpDriver driver)
        {
            if (driver == null)
                throw new System.ArgumentNullException("Driver cannot be null.");

            this.driver = driver;
            this.parent = new List<TreeNode>();
            this.children = new List<TreeNode>();
        }

        public void AddChild(TreeNode node)
        {
            if (node == null)
                throw new System.ArgumentNullException("Node cannot be null.");

            children.Add(node);

            foreach (TreeNode child in children)
                child.AddParent(this);
        }

        private void AddParent(TreeNode node)
        {
            parent.Add(node);
        }
    }
}
