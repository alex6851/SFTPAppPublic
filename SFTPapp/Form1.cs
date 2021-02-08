using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SFTPCOMINTERFACELib;
using System.Text.RegularExpressions;
using System.Collections;chm
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.Security;
using System.Security;


namespace SFTPapp
{
    public partial class Form1 : Form
    {
        CreateNewSFTPUser FTPUserCreator = new CreateNewSFTPUser();
        List<string> AllNodes = new List<string>();


        public Form1()
        {
            InitializeComponent();

            this.treeView1.CheckBoxes = true;

            this.treeView1.ShowLines = false;
            this.treeView1.ShowPlusMinus = true;

            this.treeView1.NodeMouseDoubleClick += new TreeNodeMouseClickEventHandler(treeView1_NodeMouseDoubleClick);

        }


        private static bool IsFirstLevel(TreeNode node)

        {

            return node.Parent == null;

        }

        private static bool IsSecondLevel(TreeNode node)

        {
            return node.Parent != null && node.Parent.Parent == null;
        }

        private static bool IsThirdLevel(TreeNode node)

        {

            return node.Parent != null && node.Parent.Parent != null && node.Parent.Parent.Parent == null;

        }

        public bool IsDataTypeNode(TreeNodeCollection nodes = null, TreeNode node = null, List<TreeNode> nodelist = null)

        {
            bool LastNode = new Boolean();
            foreach (string type in FTPUserCreator.DataTypes)
            {
                Regex rgx = new Regex($@"{type}", RegexOptions.IgnoreCase);
                if (node == null && nodelist == null)
                {
                    foreach (TreeNode n in nodes)
                    {
                        LastNode = rgx.IsMatch(n.Text);
                        if (LastNode)
                            break;
                    }
                }
                else if (node == null && nodes == null)
                {
                    foreach (TreeNode n in nodelist)
                    {
                        LastNode = rgx.IsMatch(n.Text);
                        if (LastNode)
                            break;
                    }

                }
                else if (node != null)
                {
                    LastNode = rgx.IsMatch(node.Text);
                }

                if (LastNode)
                {
                    break;
                }
            }

            return LastNode;
        }

        public List<string> GetCurrentDataLevels(TreeNodeCollection nodes)
        {
            List<string> DataLevelsAlreadyExist = new List<string>();

            foreach (TreeNode node in nodes)
            {
                bool IsClassLevel = IsDataTypeNode(null, node);

                if (IsClassLevel)
                {
                    if (!DataLevelsAlreadyExist.Contains(node.Text))
                    {
                        DataLevelsAlreadyExist.Add(node.Text);
                    }
                }
            }
            return DataLevelsAlreadyExist;
        }

        public List<string> GetDataLevelsthatDontExist(TreeNodeCollection nodes)
        {
            List<string> CurrentDataLevels = GetCurrentDataLevels(nodes);
            List<string> LevelsThatDontExist = new List<string>();

            foreach (string level in FTPUserCreator.DataTypes)
            {
                if (!CurrentDataLevels.Contains(level))
                {
                    LevelsThatDontExist.Add(level);
                }
            }
            return LevelsThatDontExist;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || textBox3.Text == "")
            {
                textBox6.Text = "All fields must be completed";
            }
            else
            {

                List<TreeNode> SelectedNodes = new List<TreeNode>();
                SelectedNodes = GetCheckedNodes(treeView1.Nodes, SelectedNodes);
                bool DataTypeSelected = IsDataTypeNode(null, null, SelectedNodes);
                bool MercEmail = FTPUserCreator.IsMercEmail(textBox1.Text);
                bool MultipleCompaniesSelected = AreMultipleCompaniesSelected(SelectedNodes);
                bool EmailMatchesUserName = textBox1.Text == textBox3.Text;
                bool UserExist = new Boolean();
                try
                {
                    UserExist = FTPUserCreator.DoesUserExist(textBox3.Text);
                }
                catch
                {
                    FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                    UserExist = FTPUserCreator.DoesUserExist(textBox3.Text);
                }

                if (MercEmail)
                {
                    if (!FTPUserCreator.IsValidMercUsername(textBox3.Text))
                    {
                        textBox6.Text = "This is a Mercury Email Address please enter a Mercury Username";

                    }
                    else
                    {

                        if (!DataTypeSelected)
                        {
                            textBox6.Text = "You must select at least one datatype";

                        }
                        else if (DataTypeSelected && UserExist)
                        {
                            textBox6.Text = "User already Exists";

                        }
                        else if (DataTypeSelected && !UserExist)
                        {
                            try
                            {
                                FTPUserCreator.CreateUser(textBox3.Text, "Group Associates", textBox1.Text);
                            }
                            catch
                            {
                                FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                                FTPUserCreator.CreateUser(textBox3.Text, "Group Associates", textBox1.Text);
                            }

                            
                            AssignPermissionsFromTreeView(textBox3.Text, SelectedNodes);
                            FTPUserCreator.SetHomeFolder(textBox3.Text);
                        }
                    }
                }

                else if (!EmailMatchesUserName)
                {
                    textBox6.Text = "Username and Email must be the same for all Non-Mercury Employees";

                }
                else if (EmailMatchesUserName && !MercEmail)
                {

                    if (!DataTypeSelected)
                    {
                        textBox6.Text = "You must select at least one datatype";

                    }
                    else if (DataTypeSelected && MultipleCompaniesSelected)
                    {
                        textBox6.Text = "You can only Assign permissions to one company folder for Non-Mercury Personnel";

                    }
                    else if (DataTypeSelected && !MultipleCompaniesSelected && UserExist)
                    {
                        textBox6.Text = "User Already Exists";

                    }
                    else if (DataTypeSelected && !MultipleCompaniesSelected && !UserExist)
                    {
                        string SettingsLevel = GetSettingsLevelFromNodes(SelectedNodes);
                        FTPUserCreator.CreateUser(textBox3.Text, SettingsLevel, textBox1.Text);
                        TreeNode node = GetHomeFolderNode(SelectedNodes);
                        List<Folder> FoundFolders = Folder.Find(node.FullPath);
                        Folder HomeFolder = FoundFolders[0];
                        FTPUserCreator.SetHomeFolder(textBox3.Text, HomeFolder.Fullname);
                        AssignPermissionsFromTreeView(textBox3.Text, SelectedNodes);
                        textBox1.Text = null;
                        textBox3.Text = null;
                    }

                }
            }


        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {

            if (IsSecondLevel(e.Node) || (IsThirdLevel(e.Node)) && !(IsDataTypeNode(null, e.Node)))
            {
                Folder folder = Folder.Create(e.Node.Text,e.Node.Parent.Text,e.Node.FullPath);
                List<Folder> subfolders = new List<Folder>();
                try
                {
                    subfolders = FTPUserCreator.GetSubFolders(folder);
                }
                catch
                {
                    FTPUserCreator.Connect(FTPUserCreator.UserName,FTPUserCreator.Password);
                    try
                    {
                        subfolders = FTPUserCreator.GetSubFolders(folder);
                    }
                    catch(Exception err)
                    {
                        textBox6.Text = err.Message; 
                    }

                }
                
                if (subfolders.Count != 0)
                {
                    foreach (Folder subf in subfolders)
                    {
                        if (!(AllNodes.Contains(subf.Fullname)))
                        {
                            TreeNode node = e.Node.Nodes.Add(subf.Name);
                            subf.node = node;
                            AllNodes.Add(subf.Fullname);

                        }

                    }
                }

            }
            if (IsDataTypeNode(e.Node.Nodes))
            {
                Create_AddDataTypesMenu(e.Node);
            }

        }

        public void Create_AddDataTypesMenu(TreeNode node)
        {
            ToolStripMenuItem CreateFolders = new ToolStripMenuItem();
            CreateFolders.Text = "Add DataTypes";
            CreateFolders.BackColor = SystemColors.ControlLight;
            CreateFolders.Font = new Font(this.Font, FontStyle.Underline | FontStyle.Bold);

            ToolStripMenuItem PermissionTypesItem = new ToolStripMenuItem();
            PermissionTypesItem.Text = "Set Permissions";

            ToolStripComboBox PermissionTypesCombo = new ToolStripComboBox();
            PermissionTypesCombo.Items.AddRange(new object[] {
                "Modify",
                "Upload/Download",
                "Download",
                "Upload"});
            PermissionTypesItem.DropDownItems.Add(PermissionTypesCombo);
            List<string> list = GetDataLevelsthatDontExist(node.Nodes);
            AddDataTypesMenuItem.DropDownItems.Clear();
            foreach (string member in list)
            {
                ToolStripMenuItem MenuItem = new ToolStripMenuItem(member);
                MenuItem.CheckOnClick = true;
                MenuItem.Click += new EventHandler(MenuStayOpen);
                AddDataTypesMenuItem.DropDownItems.Insert(0, MenuItem);

                AddDataTypesMenuItem.DropDownItems.Add(PermissionTypesItem);

                AddDataTypesMenuItem.DropDownItems.Add(CreateFolders);
                CreateFolders.Click += new EventHandler(AddDataTypes_Click);
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                textBox6.Text = "";
                if (e.Node.FirstNode != null)
                {

                    if (IsDataTypeNode(e.Node.Nodes))
                    {
                        contextMenuStrip1.Show(Cursor.Position);
                        treeView1.SelectedNode = e.Node;
                    }
                }
                if (e.Node.FirstNode != null)
                {
                    if (!IsDataTypeNode(e.Node.Nodes) && IsSecondLevel(e.Node))
                    {
                        contextMenuStrip2.Show(Cursor.Position);
                        toolStripTextBox1.Text = null;
                        treeView1.SelectedNode = e.Node;
                    }
                }
                if (e.Node.Level == 0)
                {
                    contextMenuStrip3.Show(Cursor.Position);
                    toolStripTextBox4.Text = null;
                    toolStripTextBox6.Text = null;
                    treeView1.SelectedNode = e.Node;
                }
                if (IsDataTypeNode(e.Node.Nodes))
                {
                    Create_AddDataTypesMenu(e.Node);
                }
            }

        }

        void MenuStayOpen(object sender, EventArgs e)
        {
            contextMenuStrip1.Show();
            AddDataTypesMenuItem.DropDown.Show();
        }
        void Menu2StayOpen(object sender, EventArgs e)
        {
            contextMenuStrip2.Show();
            createProjectFolderToolStripMenuItem.DropDown.Show();
            toolStripMenuItem1.DropDown.Show();
        }
        void Menu3StayOpen(object sender, EventArgs e)
        {
            contextMenuStrip3.Show();
            toolStripMenuItem4.DropDown.Show();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem Item in toolStripMenuItem1.DropDownItems)
            {
                if (Item.Text != "Create Project")
                {
                    Item.Click += new EventHandler(Menu2StayOpen);
                }
            }
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            foreach (ToolStripItem Item in toolStripMenuItem4.DropDownItems)
            {
                if (Item.Text != "Create Company")
                {
                    Item.Click += new EventHandler(Menu3StayOpen);
                }
            }
        }

        private void AddDataTypes_Click(object sender, EventArgs e)
        {
            textBox6.Text = "";
            List<Folder> NewFolders = new List<Folder>();
            List<SecurityGroup> SecurityGroups = new List<SecurityGroup>();
            string Company = string.Empty;
            string Project = string.Empty;
            ToolStripComboBox PermissionsComboBox3 = new ToolStripComboBox();

            foreach(ToolStripMenuItem item in AddDataTypesMenuItem.DropDownItems)
            {
                if(item.Text == "Set Permissions")
                {
                    ToolStripItem Combo = item.DropDownItems[0];
                    PermissionsComboBox3 = (ToolStripComboBox)Combo;
                }
            }

            if(PermissionsComboBox3.Text == "")
            {
                textBox6.Text = "You have to select a Permissions Level";
            }

            else if(PermissionsComboBox3.Text != "")
            {
                foreach (ToolStripMenuItem Item in AddDataTypesMenuItem.DropDownItems)
                {
                    if (Item.Text != "Add Folders")
                    {

                        if (Item.Checked)
                        {
                            Folder DataTypeFolder = Folder.Create(Item.Text, treeView1.SelectedNode.Text, $@"{treeView1.SelectedNode.FullPath}/{Item.Text}/");
                            try
                             {
                                
                                FTPUserCreator.CreateFolder(DataTypeFolder.Fullname);
                             }
                            catch
                            {
                                FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                                try
                                {

                                    FTPUserCreator.CreateFolder(DataTypeFolder.Fullname);
                                }
                                catch (Exception er)
                                {
                                    textBox6.Text = er.Message;
                                }

                            }
                            
                            if (!(AllNodes.Contains(DataTypeFolder.Fullname)))
                            {
                                
                                DataTypeFolder.node = treeView1.SelectedNode.Nodes.Add(Item.Text);
                                AllNodes.Add(DataTypeFolder.Fullname);
                                NewFolders.Add(DataTypeFolder);

                                if (treeView1.SelectedNode.Parent.Parent == null)
                                {
                                    Company = treeView1.SelectedNode.Text;
                                    Project = null;
                                }
                                else
                                {
                                    Company = treeView1.SelectedNode.Parent.Text;
                                    Project = treeView1.SelectedNode.Text;
                                }
                                SecurityGroup Group = SecurityGroup.Create(Company, Project, Item.Text);
                                FTPUserCreator.CreateSecurityGroup(Group);

                                FTPUserCreator.SetDefaultPermissions(DataTypeFolder.Fullname, Item.Text);
                                FTPUserCreator.SetPermissions(DataTypeFolder.Fullname, Group.Name, PermissionsComboBox3.Text);
                                FTPUserCreator.SetReadOnlyPermissions(DataTypeFolder.node.Parent.FullPath, Group.Name);
                                

                                foreach (TreeNode n in treeView1.SelectedNode.Nodes)
                                {
                                    if (Item.Text == "Non-ITAR" && n.Text == "ITAR")
                                    {
                                        List<Folder> FoundFolders = Folder.Find(n.FullPath);
                                        Folder ItarFolder = FoundFolders[0];
                                        List<SecurityGroup> groups = FTPUserCreator.GetSecurityGroups(ItarFolder);
                                        foreach (SecurityGroup group in groups)
                                        {
                                            FTPUserCreator.SetPermissions(DataTypeFolder.Fullname, group.Name, PermissionsComboBox3.Text);
                                        }
                                    }
                                    if (Item.Text == "ITAR" && n.Text == "Non-ITAR")
                                    {
                                        List<Folder> FoundFolders = Folder.Find(n.FullPath);
                                        Folder NonItarFolder = FoundFolders[0];
                                        FTPUserCreator.SetPermissions(NonItarFolder.Fullname, Group.Name, PermissionsComboBox3.Text);
                                    }
                                    if (Item.Text == "Non-CUI" && n.Text == "CUI")
                                    {
                                        List<Folder> FoundFolders = Folder.Find(n.FullPath);
                                        Folder CUIFolder = FoundFolders[0];
                                        List<SecurityGroup> groups = FTPUserCreator.GetSecurityGroups(CUIFolder);
                                        foreach (SecurityGroup group in groups)
                                        {
                                            FTPUserCreator.SetPermissions(DataTypeFolder.Fullname, group.Name, PermissionsComboBox3.Text);
                                        }
                                    }
                                    if (Item.Text == "CUI" && n.Text == "Non-CUI")
                                    {
                                        List<Folder> FoundFolders = Folder.Find(n.FullPath);
                                        Folder NonCUIFolder = FoundFolders[0];
                                        FTPUserCreator.SetPermissions(NonCUIFolder.Fullname, Group.Name, PermissionsComboBox3.Text);
                                    }
                                }
                            }
                            Item.Checked = false;
                        }

                    }
                }
            }

        }

        List<TreeNode> GetCheckedNodes(TreeNodeCollection nodes, List<TreeNode> list)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Checked && !IsFirstLevel(node) && !list.Contains(node))
                    list.Add(node);

                if (node.Checked && IsDataTypeNode(null, node))
                {
                    if (!list.Contains(node.Parent))
                    {
                        list.Add(node.Parent);
                    }
                    if (node.Parent.Parent != null && node.Parent.Parent.Level != 0 && !list.Contains(node.Parent.Parent))
                    {
                        list.Add(node.Parent.Parent);
                    }
                }

                GetCheckedNodes(node.Nodes, list);
            }
            return list;
        }

        public string GetSettingsLevelFromNodes(List<TreeNode> SelectedNodes)
        {
            TreeNode node = SelectedNodes[0];

            while (node.Parent != null)
            {
                node = node.Parent;
            }
            return node.Text;
        }

        public TreeNode GetHomeFolderNode(List<TreeNode> SelectedNodes)
        {
            TreeNode HomeFolderNode = new TreeNode();
            foreach (TreeNode node in SelectedNodes)
            {
                if (node.Level == 1)
                    HomeFolderNode = node;
            }

            return HomeFolderNode;
        }
        public TreeNode GetCompanyNode(TreeNode node)
        {
            while (node.Level != 1)
            {
                node = node.Parent;
            }
            return node;
        }

        public bool DoesHomeFolderMatchTreeNode(string UserName, List<TreeNode> SelectedNodes)
        {
            string HomefolderPath = FTPUserCreator.GetHomeFolder(UserName);
            TreeNode HomeFolderNode = GetHomeFolderNode(SelectedNodes);
            List<Folder> FoundFolders = Folder.Find(HomeFolderNode.FullPath);
            Folder Homefolder = FoundFolders[0];
            

            if (HomefolderPath == Homefolder.Fullname)
                return true;
            else
                return false;
        }

        public bool AreMultipleCompaniesSelected(List<TreeNode> SelectedNodes)
        {
            List<TreeNode> CompanyNodes = new List<TreeNode>();
            foreach (TreeNode node in SelectedNodes)
                if (node.Level == 1)
                    CompanyNodes.Add(node);


            if (CompanyNodes.Count > 1)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private void AssignPermissionsButton_Click(object sender, EventArgs e)
        {
            textBox6.Text = "";
            List<TreeNode> SelectedNodes = new List<TreeNode>();
            bool UserExist = new Boolean();
            SelectedNodes = GetCheckedNodes(treeView1.Nodes, SelectedNodes);
            try
            {
                UserExist = FTPUserCreator.DoesUserExist(textBox4.Text);
            }
            catch
            {
                FTPUserCreator.Connect(FTPUserCreator.UserName,FTPUserCreator.Password);
                try
                {
                    UserExist = FTPUserCreator.DoesUserExist(textBox4.Text);
                }
                catch(Exception er)
                {
                    textBox6.Text = er.Message;
                }
                
            }
            
            bool MercEmail = FTPUserCreator.IsMercEmail(textBox4.Text);
            bool MultipleCompaniesSelected = AreMultipleCompaniesSelected(SelectedNodes);

            bool DataTypeNodeSelected = IsDataTypeNode(null, null, SelectedNodes);


            if (MercEmail)
            {
                textBox6.ForeColor = Color.Red;
                textBox6.Text = "For Mercury users input their username not their Emailaddress";
            }
            else if (!UserExist)
            {
                textBox6.ForeColor = Color.Red;
                textBox6.Text = "User does not Exist";
            }
            else if (UserExist)
            {
                bool GroupAssociate = new Boolean();
                try
                {
                     GroupAssociate = FTPUserCreator.IsGroupAssociate(textBox4.Text);
                }
                catch
                {
                    FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                    try
                    {
                         GroupAssociate = FTPUserCreator.IsGroupAssociate(textBox4.Text);
                    }
                    catch(Exception er)
                    {
                        textBox6.Text = er.Message;
                    }
                }
                
                if (!DataTypeNodeSelected)
                {
                    textBox6.Text = "At least one DataType folder must be selected";
                }
                else if (GroupAssociate)
                {
                    AssignPermissionsFromTreeView(textBox4.Text, SelectedNodes);
                }
                else if (!GroupAssociate && MultipleCompaniesSelected)
                {
                    textBox6.ForeColor = Color.Red;
                    textBox6.Text = "Cannot select Multiple Companies for Non-Group Associate Users.";
                }
                else if (!GroupAssociate && !MultipleCompaniesSelected)
                {
                    if (!DoesHomeFolderMatchTreeNode(textBox4.Text, SelectedNodes))
                    {
                        string HomeFolderPath = FTPUserCreator.GetHomeFolder(textBox4.Text);
                        List<Folder> FoundFolders = Folder.Find(HomeFolderPath);

                            
                            TreeNode homefoldernode = GetHomeFolderNode(SelectedNodes);
                            string homefoldernodepath = homefoldernode.FullPath.Replace(@"\", "/");
                            if (homefoldernodepath != "/" && FoundFolders.Count != 0)
                            {
                            List<Folder> found = Folder.Find(homefoldernode.FullPath);
                                Folder homefolder = found[0];
                                homefolder.node = homefoldernode;
                                homefolder.node.Checked = true;
                                treeView1.SelectedNode = homefolder.node;
                                HomeFolderPath = homefolder.Fullname;

                            }
                            else if (FoundFolders.Count == 0)
                            {
                                HomeFolderPath = "";
                            }
                            else if (homefoldernodepath == "/")
                            {
                                textBox6.Text = "Home Folder is Root";
                            }
                            textBox6.ForeColor = Color.Red;
                            textBox6.Text = "Do you want to change the homefolder for this user?";
                            textBox6.Text += "\r\n";
                            textBox6.Text += $"Current HomeFolder: {HomeFolderPath}";
                            textBox6.Text += "\r\n";
                            textBox6.Text += $"HomeFolder changing to: {homefoldernode.FullPath}";
                            panel2.Show();
                        
                        
                    }
                    else
                    {
                        AssignPermissionsFromTreeView(textBox4.Text, SelectedNodes);
                    }
                }
            }
        }

        public void AssignPermissionsFromTreeView(string username, List<TreeNode> Nodes)
        {


            List<SecurityGroup> groups = new List<SecurityGroup>();
            List<string> groupnames = new List<string>();

            foreach (TreeNode node in Nodes)
            {
                if (node.Checked || node.Level == 1)
                {
                    List<Folder> FoundFolders = Folder.Find(node.FullPath);
                    Folder f = FoundFolders[0];
                    List<SecurityGroup> foundgroups = FTPUserCreator.GetSecurityGroups(f);
                    foreach (SecurityGroup g in foundgroups)
                    {
                       
                        if (foundgroups.Count > 1)
                        {
                            if (node.Text == "Non-ITAR" || node.Text == "Non-CUI")
                            {
                                Regex nonITARrgx = new Regex($@".*Non-ITAR.*", RegexOptions.IgnoreCase);
                                Regex nonCUIrgx = new Regex($@".*Non-CUI.*", RegexOptions.IgnoreCase);
                                bool matchNonITAR = nonITARrgx.IsMatch(g.Name);
                                bool matchNonCUI = nonCUIrgx.IsMatch(g.Name);
                                if (matchNonITAR)
                                {
                                    if (!groupnames.Contains(g.Name))
                                    {
                                        groupnames.Add(g.Name);
                                        groups.Add(g);
                                    }
                                }
                                else if (matchNonCUI)
                                {
                                    if (!groupnames.Contains(g.Name))
                                    {
                                        groupnames.Add(g.Name);
                                        groups.Add(g);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (!groupnames.Contains(g.Name))
                            {
                                groupnames.Add(g.Name);
                                groups.Add(g);
                            }
                        }

                    }
                    node.Checked = false;
                }
                if(node.Level != 1 && node.Checked)
                {

                }
                
            }
            textBox6.Text = null;
            foreach (SecurityGroup group in groups)
            {
                if (FTPUserCreator.IsUserAlreadyMember(username, group.Name))
                {
                    textBox6.ForeColor = Color.Black;
                    textBox6.Text += $"{username} already has permisisons to {group.Folder} through {group.Name}";
                    textBox6.Text += "\r\n";
                }
                else
                {
                    FTPUserCreator.AssignUsertoGroup(username, group.Name);
                    textBox6.Text += $"{textBox4.Text} assigned permissions to {group.Folder} through {group.Name}";
                    textBox6.Text += "\r\n";
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            textBox6.Text = "";
            List<TreeNode> SelectedNodes = new List<TreeNode>();
            SelectedNodes = GetCheckedNodes(treeView1.Nodes, SelectedNodes);
            TreeNode node = GetHomeFolderNode(SelectedNodes);

            List<Folder> FoundFolders = Folder.Find(node.FullPath);
            Folder homefolder = FoundFolders[0];
            homefolder.node = node;
            
            try
            {
                FTPUserCreator.SetHomeFolder(textBox4.Text, homefolder.Fullname);
            }
            catch
            {
                FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                try
                {
                    FTPUserCreator.SetHomeFolder(textBox4.Text, homefolder.Fullname);
                }
                catch(Exception error)
                {
                    textBox6.Text = error.Message;
                }
            }
            
            try
            {
                AssignPermissionsFromTreeView(textBox4.Text, SelectedNodes);
            }
            catch
            {
                FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                try
                {
                    AssignPermissionsFromTreeView(textBox4.Text, SelectedNodes);
                }
                catch(Exception err)
                {
                    textBox6.Text = err.Message;
                }
            }
            
            homefolder.node.Checked = true;
            treeView1.SelectedNode = homefolder.node;
            panel2.Hide();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            List<TreeNode> SelectedNodes = new List<TreeNode>();
            SelectedNodes = GetCheckedNodes(treeView1.Nodes, SelectedNodes);
            foreach (TreeNode node in SelectedNodes)
            {
                node.Checked = false;
            }
            panel2.Hide();
        }

        private void GetExistingUserHomeFolderButton_Click(object sender, EventArgs e)
        {
            textBox6.Text = "";
            bool UserExists = new Boolean();
            try
            {
                 UserExists = FTPUserCreator.DoesUserExist(textBox4.Text);
            }
            catch
            {
                FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                try
                {
                    UserExists = FTPUserCreator.DoesUserExist(textBox4.Text);
                }
                catch(Exception err)
                {
                    textBox6.Text = err.Message;
                }
            }
            

            if (!UserExists)
            {
                textBox6.ForeColor = Color.Red;
                textBox6.Text = "User does not Exist";
            }
            else
            {
                bool GroupAssociate = FTPUserCreator.IsGroupAssociate(textBox4.Text);
                if (!GroupAssociate)
                {
                    string HomeFolderPath = FTPUserCreator.GetHomeFolder(textBox4.Text);
                                            
                    List<Folder> foundfolders = Folder.Find(HomeFolderPath);
                    if (foundfolders.Count == 0)
                    {
                        textBox6.Text = "Home Folder could not be found";
                    }
                    else if(foundfolders.Count > 1)
                    {
                        textBox6.Text = "Multiple Home Folders found";
                    }
                    else if(foundfolders.Count == 1)
                    {
                        Folder homefolder = foundfolders[0];
                        if (homefolder.Name != "/")
                        {
                            homefolder.node.Checked = true;
                            treeView1.SelectedNode = homefolder.node;
                        }
                        else
                        {
                            textBox6.Text = "Home Folder is Root";
                        }
                    }

                    
                }
                else if (GroupAssociate)
                {
                    textBox6.Text = "This is Group Associate and their home folders are typically ROOT";
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            bool MercEmail = FTPUserCreator.IsMercEmail(textBox1.Text);
            if (MercEmail)
            {
                textBox6.Text = "This is Group Associate and their home folders are typically ROOT";
            }
            else if (!MercEmail)
            {
                List<string> homefolders = FTPUserCreator.GetHomeFoldersofDomain(textBox1.Text);
                if (homefolders.Count > 0)
                {
                    string domain = Regex.Replace(textBox1.Text, @".*@", "");
                    textBox6.Text = $"Found the following home folders for users with the {domain} domain:";
                    textBox6.Text += "\r\n";
                }
                foreach (string folderpath in homefolders)
                {
                    textBox6.Text += "\r\n";
                    textBox6.Text += $"{folderpath}";
                }

            }
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            try
            {
                textBox6.Text = "";
                FTPUserCreator.Connect(UserNameBox.Text, PasswordBox.Text);

                panel3.Hide();
                textBox6.Show();

                button7.Show();
                label7.Show();
                textBox1.Text = "";
                groupBox1.Show();
                groupBox3.Show();
                treeView1.Show();

                Folder root = new Folder();
                root.Name = "/";
                root.Fullname = "/";
                Folder.all.Add(root);
                List<Folder> RootFolders = FTPUserCreator.GetSubFolders(root);
                foreach (Folder f in RootFolders)
                {
                    TreeNode node = treeView1.Nodes.Add(f.Name);
                    f.node = node;
                }

                TreeNodeCollection nodes = treeView1.Nodes;
                foreach (TreeNode n in nodes)
                {
                    Folder UserSettingsFolder = Folder.Create(n.Text,"/",n.FullPath);
                    foreach (Folder companyfolder in FTPUserCreator.GetSubFolders(UserSettingsFolder))
                    {
                        if (!(AllNodes.Contains(companyfolder.Fullname)))
                        {
                           
                            TreeNode node = n.Nodes.Add(companyfolder.Name);
                            companyfolder.node = node;
                            AllNodes.Add(companyfolder.Fullname);
                        }
                    }
                }
            }
            catch(Exception er)
            {
                textBox6.Text = er.Message;
                textBox6.Show();
            }

            
        }

        private void CreateCompany_Click(object sender, EventArgs e)
        {
            textBox6.Text = "";
            if (toolStripTextBox4.Text == "")
            {
                textBox6.Text = "Company Name cannot be empty.";
                textBox6.ForeColor = Color.DarkRed;
            }
            else if (PermissionsComboBox2.Text == "")
            {
                textBox6.Text = "You have to select a Permissions Level";
            }
            else if(toolStripTextBox4.Text != "" && PermissionsComboBox2.Text != "")
            {
                List<Folder> FoundFolders = Folder.FindByName(toolStripTextBox4.Text);

                if (FoundFolders.Count > 0)
                {
                    textBox6.Text = "Folder found with that name in:";
                    textBox6.Text += "\r\n";
                    foreach (Folder f in FoundFolders)
                    {
                        textBox6.Text += $"{f.Fullname}";
                        textBox6.Text += "\r\n";
                    }

                }
                else
                {
                    bool GroupExists = new Boolean();
                    try
                    {
                        GroupExists = FTPUserCreator.DoesGroupExistForCompany(toolStripTextBox4.Text);
                    }
                    catch
                    {
                        FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                        GroupExists = FTPUserCreator.DoesGroupExistForCompany(toolStripTextBox4.Text);

                    }


                    if (GroupExists)
                    {
                        textBox6.Text = "Security Group already exists with Company Name";
                    }
                    else
                    {

                        List<Folder> DataTypeFolders = new List<Folder>();

                        FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                        DataTypeFolders = CreateNewCompany(treeView1.SelectedNode.Text, toolStripTextBox4.Text, toolStripMenuItem4.DropDownItems, toolStripTextBox6.Text);


                        Folder CompanyFolder = Folder.Create(toolStripTextBox4.Text,treeView1.SelectedNode.Text,$"{treeView1.SelectedNode.FullPath}/{toolStripTextBox4.Text}/","Non-ITAR");
                        

                         CompanyFolder.node = treeView1.SelectedNode.Nodes.Add(toolStripTextBox4.Text);
                         treeView1.SelectedNode = CompanyFolder.node;

                        
                        
                        if (toolStripTextBox6.Text != "")
                        {
                            Folder ProjectFolder = Folder.Create(toolStripTextBox4.Text, treeView1.SelectedNode.Text, $"{treeView1.SelectedNode.FullPath}/{toolStripTextBox4.Text}/{toolStripTextBox6.Text}/", "ITAR");

                            ProjectFolder.node = CompanyFolder.node.Nodes.Add(toolStripTextBox6.Text);
                            treeView1.SelectedNode = ProjectFolder.node;
                           
                               
                            
                            foreach (Folder f in DataTypeFolders)
                            {
                                if(!AllNodes.Contains(f.Fullname))
                                {
                                    f.node = ProjectFolder.node.Nodes.Add(f.Name);
                                    treeView1.SelectedNode = f.node;
                                    AllNodes.Add(f.Fullname);
                                } 
                            }
                        }
                        else
                        {
                            foreach (Folder f in DataTypeFolders)
                            {
                                if(!AllNodes.Contains(f.Fullname))
                                {
                                    CompanyFolder.node.Nodes.Add(f.Name);
                                    AllNodes.Add(f.Fullname);
                                }
                            }
                        }
                    }
                }

            }
        }

        private void CreateProject_Click(object sender, EventArgs e)
        {
            textBox6.Text = "";
            if (toolStripTextBox1.Text == "")
            {
                textBox6.Text = "Project Name cannot be empty";

            }
            else if(PermissionsComboBox1.Text == "")
            {
                textBox6.Text = "You have to select a Permissions Level";
            }
            else if(toolStripTextBox1.Text != "" && PermissionsComboBox1.Text != "")
            {

                string CompanyName = treeView1.SelectedNode.Text;
                string ProjectName = toolStripTextBox1.Text;

                Folder ProjectFolder = Folder.Create(ProjectName, CompanyName, $@"{treeView1.SelectedNode.FullPath}/{ProjectName}/");

                ProjectFolder.node = treeView1.SelectedNode.Nodes.Add(ProjectName);
                AllNodes.Add(ProjectFolder.Fullname);

                List<Folder> NewFolders = new List<Folder>();

                try
                {
                    FTPUserCreator.CreateFolder(ProjectFolder.Fullname);

                }
                catch
                {
                    FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                    try
                    {
                        FTPUserCreator.CreateFolder(ProjectFolder.Fullname);
                    }
                    catch (Exception er)
                    {
                        textBox6.Text = er.Message;
                    }

                }
                try
                {
                    FTPUserCreator.SetDefaultPermissions(ProjectFolder.Fullname, "Non-ITAR");
                }
                catch
                {
                    FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                    try
                    {
                        FTPUserCreator.SetDefaultPermissions(ProjectFolder.Fullname, "Non-ITAR");
                    }
                    catch(Exception er)
                    {
                        textBox6.Text = er.Message;
                    }
                }
                
                FTPUserCreator.DisableInheritPermissions(ProjectFolder.Fullname);
                NewFolders.Add(ProjectFolder);
                treeView1.SelectedNode = ProjectFolder.node;

               List<SecurityGroup> SecurityGroups = CreateDataTypeFolders(ProjectFolder, CompanyName, toolStripMenuItem1.DropDownItems, ProjectFolder.Name,PermissionsComboBox1.Text);

                
                foreach(SecurityGroup g in SecurityGroups)
                {
                    FTPUserCreator.SetReadOnlyPermissions(ProjectFolder.Fullname, g.Name);
                    if(!AllNodes.Contains(g.Folder.Fullname))
                    {
                        NewFolders.Add(g.Folder);
                        g.Folder.node = ProjectFolder.node.Nodes.Add(g.Folder.Name);
                        treeView1.SelectedNode = g.Folder.node;
                        AllNodes.Add(g.Folder.Fullname);
                    }

                }
                FTPUserCreator.AssignGroupstoFolders(NewFolders, SecurityGroups,PermissionsComboBox1.Text);
            }

        }

        public List<Folder> CreateNewCompany(string SettingsLevel, string CompanyName, ToolStripItemCollection DataTypes, string ProjectName)
        {
            SecurityGroup CompanyGroup = new SecurityGroup();
            
            List<SecurityGroup> NewSecurityGroups = new List<SecurityGroup>();
            Folder Folder = Folder.Create(CompanyName, SettingsLevel,$"{SettingsLevel}/{CompanyName}/","Non-ITAR");
            AllNodes.Add(Folder.Fullname);
            try
            {
                FTPUserCreator.CreateFolder(Folder.Fullname);
            }
            catch
            {
                FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
               try
                {
                    FTPUserCreator.CreateFolder(Folder.Fullname);

                }
                catch(Exception err)
                {
                    textBox6.Text = err.Message;
                }
            }
            

            Folder CompanyFolder = Folder;
            try
            {
                FTPUserCreator.SetDefaultPermissions(CompanyFolder.Fullname, "Non-ITAR");
            }
            catch
            {
                FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                try
                {
                    FTPUserCreator.SetDefaultPermissions(CompanyFolder.Fullname, "Non-ITAR");
                }
                catch(Exception er)
                {
                    textBox6.Text = er.Message;
                }
            }

            var PFolder = new object();
            if (ProjectName != "")
            {

                string ProjectFolderPath = FTPUserCreator.CreateFolderPath(SettingsLevel, CompanyName, ProjectName);

                Folder = Folder.Create(ProjectName, CompanyName, ProjectFolderPath);
                FTPUserCreator.CreateFolder(Folder.Fullname);

                PFolder = (Folder)Folder;
                AllNodes.Add(Folder.Fullname);
                

                CompanyGroup = SecurityGroup.Create(CompanyName);
                CompanyGroup.Folder = Folder;
                FTPUserCreator.CreateSecurityGroup(CompanyGroup);
                NewSecurityGroups.Add(CompanyGroup);

                FTPUserCreator.SetDefaultPermissions(Folder.Fullname, "Non-ITAR");

                FTPUserCreator.SetReadOnlyPermissions(CompanyFolder.Fullname, CompanyGroup.Name);
            }
            Folder ProjectFolder = PFolder as Folder;


            List<SecurityGroup> DataTypeGroups = CreateDataTypeFolders(Folder, CompanyName, DataTypes, ProjectName,PermissionsComboBox2.Text);
            NewSecurityGroups.AddRange(DataTypeGroups);

            foreach (SecurityGroup group in NewSecurityGroups)
            {

                if (ProjectName == "")
                {
                    FTPUserCreator.SetReadOnlyPermissions(CompanyFolder.Fullname, group.Name);
                }

                else
                {
                    FTPUserCreator.SetReadOnlyPermissions(CompanyFolder.Fullname, CompanyGroup.Name);

                }

            }

            foreach (SecurityGroup group in NewSecurityGroups)
            {
                if (group.Name != CompanyName && ProjectName != "")
                {
                    FTPUserCreator.SetReadOnlyPermissions(ProjectFolder.Fullname, group.Name);
                }
            }

            List<Folder> DataTypeFolders = new List<Folder>();
            foreach(SecurityGroup g in DataTypeGroups)
            {
                if(!DataTypeFolders.Contains(g.Folder))
                {
                    DataTypeFolders.Add(g.Folder);
                }
            }

            return DataTypeFolders;
        }

        public List<SecurityGroup> CreateDataTypeFolders(Folder Folder, string CompanyName, ToolStripItemCollection DataTypes, string ProjectName,string PermissionsLevel)
        {

            List<string> SelectedDataTypes = new List<string>();
            List<Folder> NewFolders = new List<Folder>();
            List<SecurityGroup> NewSecurityGroups = new List<SecurityGroup>();

            foreach (ToolStripMenuItem t in DataTypes)
            {
                if (t.Checked)
                {
                    SelectedDataTypes.Add(t.Text);
                    t.Checked = false;
                }
            }
            if (SelectedDataTypes.Contains("ITAR"))
            {
                Folder ITARFolder = Folder.Create("ITAR", Folder.Name, $"{Folder.Fullname}/ITAR/");
                try
                {
                    FTPUserCreator.CreateFolder(ITARFolder.Fullname);
                }
                catch
                {
                    FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                    try
                    {   
                        FTPUserCreator.CreateFolder(ITARFolder.Fullname);  
                    }
                    catch(Exception e)
                    {
                        textBox6.Text = e.Message; 
                    }
                                
                }
                NewFolders.Add(ITARFolder);

                
                Folder NonITARFolder = Folder.Create("Non-ITAR", Folder.Name, $"{Folder.Fullname}/Non-ITAR/");
                try
                {
                    FTPUserCreator.CreateFolder(NonITARFolder.Fullname);

                }
                catch
                {
                    FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                    try
                    {
                        FTPUserCreator.CreateFolder(NonITARFolder.Fullname);
                    }
                    catch (Exception e)
                    {
                        textBox6.Text = e.Message;
                    }

                }
                
                NewFolders.Add(NonITARFolder);

                SecurityGroup nonitargroup = SecurityGroup.Create(CompanyName, ProjectName, "Non-ITAR");
                nonitargroup.Folder = NonITARFolder;
                SecurityGroup itargroup = SecurityGroup.Create(CompanyName, ProjectName, "ITAR");
                itargroup.Folder = ITARFolder;

                FTPUserCreator.CreateSecurityGroup(itargroup);
                NewSecurityGroups.Add(itargroup);

                FTPUserCreator.CreateSecurityGroup(nonitargroup);
                NewSecurityGroups.Add(nonitargroup);
                try
                {
                    FTPUserCreator.SetDefaultPermissions($"{Folder.Fullname}/ITAR", "ITAR");
                }
                catch
                {
                    FTPUserCreator.Connect(FTPUserCreator.UserName, FTPUserCreator.Password);
                    try
                    {
                        FTPUserCreator.SetDefaultPermissions($"{Folder.Fullname}/ITAR", "ITAR");
                    }
                    catch(Exception er)
                    {
                        textBox6.Text = er.Message; 

                    }

                }
                
                FTPUserCreator.SetPermissions($"{Folder.Fullname}/ITAR", itargroup.Name,PermissionsLevel);

                FTPUserCreator.SetDefaultPermissions($"{Folder.Fullname}/Non-ITAR", "Non-ITAR");
                FTPUserCreator.SetPermissions($"{Folder.Fullname}/Non-ITAR", nonitargroup.Name,PermissionsLevel);
                FTPUserCreator.SetPermissions($"{Folder.Fullname}/Non-ITAR", itargroup.Name,PermissionsLevel);
            }


            if (SelectedDataTypes.Contains("Non-ITAR") && !SelectedDataTypes.Contains("ITAR"))
            {
                Folder NonITARFolder = Folder.Create("Non-ITAR", Folder.Name, $"{Folder.Fullname}/Non-ITAR/");
                FTPUserCreator.CreateFolder(NonITARFolder.Fullname);

                NewFolders.Add(NonITARFolder);

                SecurityGroup nonitargroup = SecurityGroup.Create(CompanyName, ProjectName, "Non-ITAR");
                nonitargroup.Folder = NonITARFolder;

                FTPUserCreator.CreateSecurityGroup(nonitargroup);
                NewSecurityGroups.Add(nonitargroup);

                FTPUserCreator.SetDefaultPermissions($"{Folder.Fullname}/Non-ITAR", "Non-ITAR");
                FTPUserCreator.SetPermissions($"{Folder.Fullname}/Non-ITAR", nonitargroup.Name,PermissionsLevel);

            }
            if (SelectedDataTypes.Contains("CUI"))
            {
                Folder NonCuiFolder = Folder.Create("Non-CUI", Folder.Name, $"{Folder.Fullname}/Non-CUI/");
                FTPUserCreator.CreateFolder(NonCuiFolder.Fullname);

                NewFolders.Add(NonCuiFolder);

                Folder CUIfolder = Folder.Create("CUI", Folder.Name, $"{Folder.Fullname}/CUI/");
                FTPUserCreator.CreateFolder(CUIfolder.Fullname);

                NewFolders.Add(CUIfolder);

                SecurityGroup noncuigroup = SecurityGroup.Create(CompanyName, ProjectName, "Non-CUI");
                noncuigroup.Folder = NonCuiFolder;

                SecurityGroup cuigroup = SecurityGroup.Create(CompanyName, ProjectName, "CUI");
                cuigroup.Folder = CUIfolder;


                FTPUserCreator.CreateSecurityGroup(noncuigroup);
                NewSecurityGroups.Add(noncuigroup);

                FTPUserCreator.CreateSecurityGroup(cuigroup);
                NewSecurityGroups.Add(cuigroup);

                FTPUserCreator.SetDefaultPermissions($"{Folder.Fullname}/Non-CUI", "Non-CUI");
                FTPUserCreator.SetPermissions($"{Folder.Fullname}/Non-CUI", noncuigroup.Name,PermissionsLevel);
                FTPUserCreator.SetPermissions($"{Folder.Fullname}/Non-CUI", cuigroup.Name,PermissionsLevel);

                FTPUserCreator.SetDefaultPermissions($"{Folder.Fullname}/CUI", "CUI");
                FTPUserCreator.SetPermissions($"{Folder.Fullname}/CUI", cuigroup.Name,PermissionsLevel);
            }
            if (SelectedDataTypes.Contains("Non-CUI") && !SelectedDataTypes.Contains("CUI"))
            {

                Folder NonCUIfolder = Folder.Create("Non-CUI", Folder.Name, $"{Folder.Fullname}/Non-CUI/");
                FTPUserCreator.CreateFolder(NonCUIfolder.Fullname);

                NewFolders.Add(NonCUIfolder);

                SecurityGroup noncuigroup = SecurityGroup.Create(CompanyName, ProjectName, "Non-CUI");
                noncuigroup.Folder = NonCUIfolder;

                FTPUserCreator.CreateSecurityGroup(noncuigroup);
                NewSecurityGroups.Add(noncuigroup);

                FTPUserCreator.SetDefaultPermissions($"{Folder.Fullname}/Non-CUI", "Non-CUI");
                FTPUserCreator.SetPermissions($"{Folder.Fullname}/Non-CUI", noncuigroup.Name,PermissionsLevel);

            }
            if (SelectedDataTypes.Contains("FOUO"))
            {
                Folder FOUOfolder = Folder.Create("FOUO", Folder.Name, $"{Folder.Fullname}/FOUO/");
                FTPUserCreator.CreateFolder(FOUOfolder.Fullname);

                NewFolders.Add(FOUOfolder);

                SecurityGroup fouogroup = SecurityGroup.Create(CompanyName, ProjectName, "FOUO");
                fouogroup.Folder = FOUOfolder;

                FTPUserCreator.CreateSecurityGroup(fouogroup);
                NewSecurityGroups.Add(fouogroup);

                FTPUserCreator.SetDefaultPermissions($"{Folder.Fullname}/FOUO", "FOUO");
                FTPUserCreator.SetPermissions($"{Folder.Fullname}/FOUO", fouogroup.Name,PermissionsLevel);

            }
            if (SelectedDataTypes.Contains("EAR"))
            {
                Folder EARFolder = Folder.Create("EAR", Folder.Name, $"{Folder.Fullname}/EAR/");
                FTPUserCreator.CreateFolder(EARFolder.Fullname);
                NewFolders.Add(EARFolder);

                SecurityGroup EARgroup = SecurityGroup.Create(CompanyName, ProjectName, "EAR");
                EARgroup.Folder = EARFolder;

                FTPUserCreator.CreateSecurityGroup(EARgroup);
                NewSecurityGroups.Add(EARgroup);

                FTPUserCreator.SetDefaultPermissions($"{Folder.Fullname}/EAR", "EAR");
                FTPUserCreator.SetPermissions($"{Folder.Fullname}/EAR", EARgroup.Name,PermissionsLevel);

            }
            return NewSecurityGroups;
        }


    }
}

public class Folder
{
    static public List<Folder> all = new List<Folder>();
    static public List<string> allpaths = new List<string>();
    public string Name;
    public string Fullname;
    public string ParentFolder;
    public string DataType;
    public TreeNode node;
    public List<string> DataTypes = new List<string>();
    public List<SecurityGroup> SecurityGroups;

    public Folder()
    {
       
    }

    static string ReplaceAtIndex(int i, char value, string word)
    {
        char[] letters = word.ToCharArray();
        letters[i] = value;
        return string.Join("", letters);
    }

    public string FormatPath(string path)
    {
        path = path.Replace(@"\", "/");
        path = path.Replace(@"\\", "/");
        return path;
    }

    public static Folder Create(string foldername, string parentfolder = null, string fullname = null, string datatype = null)
    {


        if (parentfolder == null)
        {
            fullname = "/" + foldername;
            parentfolder = "/";
        }

        fullname = fullname.Replace(@"\", "/");
        fullname = fullname.Replace(@"\\", "/");
        fullname = fullname.Replace(@"//", "/");
        var lastChar = fullname.Substring(fullname.Length - 1);
        var FirstChar = fullname.Substring(0, 1);
        if (lastChar != "/")
        {
            fullname = fullname + "/";
        }
        if (FirstChar != "/")
        {
            fullname = "/" + fullname;
        }


        Folder folder = new Folder();
        folder.Name = foldername;
        folder.ParentFolder = parentfolder;
        folder.Fullname = fullname;
        folder.DataType = datatype;
        if (datatype != null)
        {
            if (!folder.DataTypes.Contains(datatype))
            {
                folder.DataTypes.Add(datatype);
            }
        }
        if (!allpaths.Contains(fullname))
        {           
            allpaths.Add(folder.Fullname);
            all.Add(folder);
        }
              
        return folder;

    }


    public static List<Folder> Find(string Fullpath)
    {
        Fullpath = Fullpath.Replace(@"\", "/");
        Fullpath = Fullpath.Replace(@"\\", "/");



        List<Folder> FoundFolders = new List<Folder>();

        var lastChar = Fullpath.Substring(Fullpath.Length - 1);
        var FirstChar = Fullpath.Substring(0,1);
        if (lastChar != "/")
        {
            Fullpath = Fullpath + "/";
        }
        if (FirstChar != "/")
        {
            Fullpath = "/" + Fullpath;
        }

        Regex rgx = new Regex($@"^{Fullpath}$", RegexOptions.IgnoreCase);

        foreach (Folder f in Folder.all)
        {
            if (rgx.IsMatch(f.Fullname))
            {
                FoundFolders.Add(f);
            }
        }
        return FoundFolders;
    }

    public static List<Folder> FindByName(string name)
    {
        List<Folder> FoundFolders = new List<Folder>();

        Regex rgx = new Regex($@"{name}", RegexOptions.IgnoreCase);

        foreach (Folder f in Folder.all)
        {
            if (rgx.IsMatch(f.Name))
            {
                FoundFolders.Add(f);
            }
        }
        return FoundFolders;
    }

}

public class SecurityGroup
{
    static public List<SecurityGroup> all = new List<SecurityGroup>();
    public Folder Folder;
    public string Name;
    public string DataType;
    public SecurityGroup()
    {

        if (!all.Contains(this))
        {
            all.Add(this);
        }

    }

    public static SecurityGroup Create(string CompanyName = null, string ProjectName = null, string DataType = null)
    {
        SecurityGroup g = new SecurityGroup();
        if ((ProjectName == null) && (DataType == null))
        {
            g.Name = CompanyName;
        }
        else if ((ProjectName != null) && (DataType == null))
        {
            g.Name = $"{CompanyName}-{ProjectName}";
        }
        else if ((ProjectName != null) && (DataType != null))
        {
            g.Name = $"{CompanyName}-{ProjectName}-{DataType}";
            g.DataType = DataType;
        }
        else if ((ProjectName == null) && (DataType != null) && (CompanyName != null))
        {
            g.Name = $"{CompanyName}-{DataType}";
            g.DataType = DataType;
        }
        return g;
    }


}

public class CreateNewSFTPUser
{
    public List<string> SettingsLevels = new List<string>() { "Contract Manufacturers", "Customers", "Development Partners", "Support Partners" };
    public List<string> DataTypes = new List<string>() { "EAR", "FOUO", "Non-CUI", "CUI", "Non-ITAR", "ITAR" };
    public CISite SFTPsite;
    public CISite WorkSpaces;
    public string UserName;
    public string Password;


    public bool IsMercEmail(string EmailAddress)
    {
        Regex rgx = new Regex(@".*domainName.com");
        bool MercEmail = rgx.IsMatch(EmailAddress);
        return MercEmail;
    }

    public bool IsGroupAssociate(string username)
    {
        string SettingsLevel = SFTPsite.GetUserSettingsLevel(username);
        if (SettingsLevel == "Group Associates")
            return true;
        else
            return false;
    }

    public bool IsEmailaddress(string EmailAddress)
    {
        Regex rgx = new Regex(@"@.*.com");
        bool Email = rgx.IsMatch(EmailAddress);
        return Email;
    }

    public bool IsValidMercUsername(string username)
    {
        Array users = (object[])WorkSpaces.GetUsers();

        Regex rgx = new Regex($@"{username}", RegexOptions.IgnoreCase);
        bool ValidMercUserName = false;
        foreach (string user in users)
        {
            ValidMercUserName = rgx.IsMatch(user);
            if (ValidMercUserName)
            {
                break;
            }
        }
        return ValidMercUserName;
    }


    public bool DoesUserExist(string username)
    {
        bool UserExists = new Boolean();
        UserExists = SFTPsite.DoesUsernameExist(username);
        return
            UserExists;
    }

    public List<string> GetHomeFoldersofDomain(string EmailAddress)
    {
        string domain = Regex.Replace(EmailAddress, @".*@", "");
        Regex rgx = new Regex($@"{domain}", RegexOptions.IgnoreCase);
        Array users = (object[])SFTPsite.GetUsers();
        List<string> FoundUsers = new List<string>();
        List<string> FoundHomeFolders = new List<string>();

        foreach (string user in users)
        {
            if (rgx.IsMatch(user))
                FoundUsers.Add(user);
        }

        foreach (string user in FoundUsers)
        {
            FoundHomeFolders.Add(GetUserHomeFolder(user));
        }

        return FoundHomeFolders;
    }
    public string GetUserHomeFolder(string username)
    {
        CIClientSettings userSettings = SFTPsite.GetUserSettings(username);
        return userSettings.GetHomeDirString();
    }

    public void Connect(string username, string password)
    {
        UserName = username;
        Password = password;
        CIServer m_server = new CIServer();
   
        m_server.ConnectEx("servername", 1100, AdminLoginType.NetLogon, $"DomainName/{username}", $"{password}");

        

        CISites sites = m_server.Sites();

        string siteNameWork = "SiteConnectedToAD.com";
        string siteName = "SFTPsiteName.com";

        SFTPsite = null;
        WorkSpaces = null;
        for (int i = 0; i < sites.Count(); i++)
        {
            CISite site = sites.Item(i);

            if (site.Name == siteNameWork)
            {
                WorkSpaces = site;

            }
            if (site.Name == siteName)
            {
                SFTPsite = site;

            }
        }
    }

    public string CreateFolderPath(string settingslevel, string companyname, string datatype = null, string projectname = null)
    {
        string FolderPath = string.Empty;

        if (datatype == null && projectname == null)
        {
            FolderPath =  settingslevel + "/" + companyname;
        }
        else if (datatype == null)
        {
            FolderPath = settingslevel + "/" + companyname + "/" + projectname;
        }

        else if (datatype != null)
        {
            FolderPath = settingslevel + "/" + companyname + "/" + projectname + "/" + datatype;
        }
        return FolderPath;
    }

    public void CreateFolder(string fullname)
    {
        fullname = fullname.Replace(@"\", "/");
        fullname = fullname.Replace(@"\\", "/");
        fullname = fullname.Replace(@"//", "/");
        
        SFTPsite.CreatePhysicalFolder(fullname);
    }

    public bool DoesFolderExist(string path)
    {
        bool FolderExist = new Boolean();
        path = path.Replace(@"\\","/");
        path = path.Replace(@"\","/");
        FolderExist = true;
        try
        {
            SFTPsite.GetFolderList($"{path}");
        }
        catch (Exception e)
        {
            if (e.Message == "Failed to find specified folder in VFS")
            {
                FolderExist = false; 
            }
        }
        return FolderExist;
    }

    public List<Folder> GetSubFolders(Folder folder)
    {

        List<Folder> SubFolders = new List<Folder>();
        string folders = SFTPsite.GetFolderList(folder.Fullname);
        if (folders != "")
        {
            List<string> FolderList = folders.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                         .ToList();
            for (int i = 0; i < FolderList.Count; i++)
            {
                string newfoldername = FolderList[i];
                string Fullname = folder.Fullname + "/" + newfoldername + "/";
                string ParentFolder = folder.Name;

                Folder newfolder = Folder.Create(newfoldername, ParentFolder, Fullname);

                SubFolders.Add(newfolder);
            }
        }
        return SubFolders;
    }

    public List<Folder> RecursiveGetSubFolders(Folder initialFolder)
    {
        var folders = new List<Folder>();
        folders.Add(initialFolder);
        foreach (var f in GetSubFolders(initialFolder))
        {
            folders.AddRange(RecursiveGetSubFolders(f));
        }
        return folders;
    }

    public List<Permission> GetPermissionsfromFolder(string folder)
    {
        List<Permission> SecurityGroups = new List<Permission>();
        foreach (Permission perm in (object[])SFTPsite.GetFolderPermissions(folder))
        {
            if (!(SecurityGroups.Contains(perm)))
            {
                SecurityGroups.Add(perm);
            }
        }
        return SecurityGroups;
    }

    public void DisableInheritPermissions(string folderpath)
    {

        SFTPsite.DisableInheritPermissions(folderpath, true);

    }

    public List<SecurityGroup> GetSecurityGroups(Folder folder)
    {

        List<SecurityGroup> SecurityGroups = new List<SecurityGroup>();
        List<Permission> Permissions = GetPermissionsfromFolder(folder.Fullname);

        List<string> DefaultGroups = new List<string>() { "All Users", "Group ITAR", "Group Non-ITAR" };
        foreach (Permission p in Permissions)
        {
            if (p.IsGroup && !DefaultGroups.Contains(p.Client))
            {
                SecurityGroup group = new SecurityGroup();
                group.Name = p.Client;
                group.Folder = folder;
                if (!SecurityGroups.Contains(group))
                {
                    SecurityGroups.Add(group);
                }
            }
        }

        //foreach(SecurityGroup g in SecurityGroups)
        //{
        //    foreach (string type in this.DataTypes)
        //    {
        //        Regex rgx = new Regex($@".*Non-{type}$", RegexOptions.IgnoreCase);
        //        Regex rgx2 = new Regex($@".*{type}$", RegexOptions.IgnoreCase);
        //        if (rgx.IsMatch(g.Name))
        //        {
        //            g.DataType = type;
        //        }
        //        else if(rgx2.IsMatch(g.Name))
        //        {

        //        }

        //    }
        //}
        return SecurityGroups;
    }

    public void SendEmail(string EmailAddress)
    {

    }


    public void CreateUser(string username, string SettingsLevel, string EmailAddress)
    {

       
        string password = SFTPsite.CreateComplexPassword();
        ICINewUserData data = new CINewUserData();
        data.FullName = "";
        data.Login = username;
        data.Password = password;
        data.PasswordType = 0;
        data.Description = "EmailCredentials";
        data.Email = EmailAddress;
        data.CreateHomeFolder = false;
        data.SettingsLevel = SettingsLevel;
        data.TwoFactorAuthentication = SFTPAdvBool.abFalse;
        data.FullPermissionsForHomeFolder = false;


        SFTPsite.CreateUserEx2(data);


        CIClientSettings user = SFTPsite.GetUserSettings(UserName);
        user.ForcePasswordChange();
    }

    public string GetHomeFolder(string UserName)
    {
        CIClientSettings user = SFTPsite.GetUserSettings(UserName);
        string HomeDir = user.GetHomeDirString();
        return HomeDir;
    }

    public void SetHomeFolder(string UserName, string HomeFolderPath = null)
    {
        CIClientSettings user = SFTPsite.GetUserSettings(UserName);
        if (HomeFolderPath == null)
        {
            user.SetHomeDirIsRoot(SFTPAdvBool.abTrue);
            user.SetHomeDir(SFTPAdvBool.abTrue);
            user.SetHomeDirString("/");
        }
        else
        {
            HomeFolderPath = HomeFolderPath.Replace(@"\", "/");
            HomeFolderPath = HomeFolderPath.Replace(@"\\", "/");

            user.SetHomeDirIsRoot(SFTPAdvBool.abTrue);
            user.SetHomeDir(SFTPAdvBool.abTrue);
            user.SetHomeDirString($"{HomeFolderPath}");

        }
    }

    public void AssignGroupstoFolders(List<Folder> folders, List<SecurityGroup> groups,string PermissionsLevel)
    {
        foreach (Folder f in folders)
        {
            if (f.Name == "ITAR")
            {
                foreach (SecurityGroup s in groups)
                {
                    if (s.DataType == "ITAR")
                    {
                        SetPermissions(f.Fullname, s.Name,PermissionsLevel);
                    }
                }
            }
            if (f.Name == "Non-ITAR")
            {
                Regex ITARrgx = new Regex($@"ITAR", RegexOptions.IgnoreCase);

                foreach (SecurityGroup s in groups)
                {
                    bool m = ITARrgx.IsMatch(s.DataType);
                    if (m)
                    {
                        SetPermissions(f.Fullname, s.Name,PermissionsLevel);
                    }
                }
            }
            if (f.Name == "CUI")
            {
                foreach (SecurityGroup s in groups)
                {
                    if (s.DataType == "CUI")
                    {
                        SetPermissions(f.Fullname, s.Name,PermissionsLevel);
                    }
                }

            }
            if (f.Name == "Non-CUI")
            {
                Regex rgx = new Regex($@"CUI", RegexOptions.IgnoreCase);
                foreach (SecurityGroup s in groups)
                {
                    bool m = rgx.IsMatch(s.DataType);
                    if (m)
                    {
                        SetPermissions(f.Fullname, s.Name,PermissionsLevel);
                    }
                }
            }
            if (f.Name == "FOUO")
            {
                foreach (SecurityGroup s in groups)
                {
                    if (s.DataType == "FOUO")
                    {
                        SetPermissions(f.Fullname, s.Name,PermissionsLevel);
                    }
                }
            }
            if (f.Name == "EAR")
            {
                foreach (SecurityGroup s in groups)
                {
                    if (s.DataType == "EAR")
                    {
                        SetPermissions(f.Fullname, s.Name,PermissionsLevel);
                    }
                }
            }
        }
    }

    public bool IsUserAlreadyMember(string username, string group)
    {
        foreach (string user in (object[])SFTPsite.GetPermissionGroupList(group))
            if (user == username)
                return true;
        return false;
    }

    public void AssignUsertoGroup(string username, string group)
    {
        SFTPsite.AddUserToPermissionGroup(username, group);
    }


    public void CreateSecurityGroup(SecurityGroup Group = null, string groupname = null)
    {
        if (groupname == null)
        {
            SFTPsite.CreatePermissionGroup(Group.Name);
        }
        else
        {
            SFTPsite.CreatePermissionGroup(groupname);
        }

    }


    public bool DoesGroupExistForCompany(string CompanyName)
    {
        bool GroupExists = new Boolean();
        IEnumerable groups = SFTPsite.GetPermissionGroups();

        Regex rgx = new Regex($@"{CompanyName}.*", RegexOptions.IgnoreCase);
        GroupExists = false;

        foreach (string group in groups)
        {
            GroupExists = rgx.IsMatch(group);
            if (GroupExists)
            {
                break;
            }
        }
        return GroupExists;

    }

    public void SetReadOnlyPermissions(string folder, string group)
    {
        folder = folder.Replace(@"\\", "/");
        folder = folder.Replace(@"\","/");
        folder = folder.Replace(@"//", "/");

        Permission perm = SFTPsite.GetBlankPermission(folder, group);
        perm.DirCreate = false;
        perm.DirDelete = false;
        perm.DirList = true;
        perm.DirShowHidden = false;
        perm.DirShowInList = true;
        perm.DirShowReadOnly = false;
        perm.FileAppend = false;
        perm.FileDelete = false;
        perm.FileDownload = false;
        perm.FileRename = false;
        perm.FileUpload = false;
        SFTPsite.SetPermission(perm, false);
    }
    public void SetDefaultPermissions(string folder, string datatype)
    {
        folder = folder.Replace(@"//", "/");
        bool GroupISControlledDataType = new Boolean();
        List<string> ITARFileTypes = new List<string>() { "ITAR", "FOUO", "EAR", "CUI" };

        foreach (string type in ITARFileTypes)
        {
            Regex rgx = new Regex($@"^{type}$", RegexOptions.IgnoreCase);
            GroupISControlledDataType = rgx.IsMatch(datatype);
        }
        if (GroupISControlledDataType)
        {
            Permission allusersperm = SFTPsite.GetBlankPermission(folder, "All Users");
            allusersperm.DirCreate = false;
            allusersperm.DirDelete = false;
            allusersperm.DirList = false;
            allusersperm.DirShowHidden = false;
            allusersperm.DirShowInList = false;
            allusersperm.DirShowReadOnly = false;
            allusersperm.FileAppend = false;
            allusersperm.FileDelete = false;
            allusersperm.FileDownload = false;
            allusersperm.FileRename = false;
            allusersperm.FileUpload = false;
            SFTPsite.SetPermission(allusersperm, true);

            Permission Groupitar = SFTPsite.GetBlankPermission(folder, "Group ITAR");
            Groupitar.DirCreate = false;
            Groupitar.DirDelete = false;
            Groupitar.DirList = true;
            Groupitar.DirShowHidden = false;
            Groupitar.DirShowInList = true;
            Groupitar.DirShowReadOnly = false;
            Groupitar.FileAppend = false;
            Groupitar.FileDelete = false;
            Groupitar.FileDownload = false;
            Groupitar.FileRename = false;
            Groupitar.FileUpload = false;
            SFTPsite.SetPermission(Groupitar, false);

            Permission Groupnonitar = SFTPsite.GetBlankPermission(folder, "Group Non-ITAR");
            Groupnonitar.DirCreate = false;
            Groupnonitar.DirDelete = false;
            Groupnonitar.DirList = false;
            Groupnonitar.DirShowHidden = false;
            Groupnonitar.DirShowInList = false;
            Groupnonitar.DirShowReadOnly = false;
            Groupnonitar.FileAppend = false;
            Groupnonitar.FileDelete = false;
            Groupnonitar.FileDownload = false;
            Groupnonitar.FileRename = false;
            Groupnonitar.FileUpload = false;
            SFTPsite.SetPermission(Groupnonitar, false);
        }
        else
        {

            Permission allusersperm = SFTPsite.GetBlankPermission(folder, "All Users");
            allusersperm.DirCreate = false;
            allusersperm.DirDelete = false;
            allusersperm.DirList = false;
            allusersperm.DirShowHidden = false;
            allusersperm.DirShowInList = false;
            allusersperm.DirShowReadOnly = false;
            allusersperm.FileAppend = false;
            allusersperm.FileDelete = false;
            allusersperm.FileDownload = false;
            allusersperm.FileRename = false;
            allusersperm.FileUpload = false;
            SFTPsite.SetPermission(allusersperm, true);

            Permission Groupitar = SFTPsite.GetBlankPermission(folder, "Group ITAR");
            Groupitar.DirCreate = false;
            Groupitar.DirDelete = false;
            Groupitar.DirList = true;
            Groupitar.DirShowHidden = false;
            Groupitar.DirShowInList = true;
            Groupitar.DirShowReadOnly = false;
            Groupitar.FileAppend = false;
            Groupitar.FileDelete = false;
            Groupitar.FileDownload = false;
            Groupitar.FileRename = false;
            Groupitar.FileUpload = false;
            SFTPsite.SetPermission(Groupitar, false);

            Permission Groupnonitar = SFTPsite.GetBlankPermission(folder, "Group Non-ITAR");
            Groupnonitar.DirCreate = false;
            Groupnonitar.DirDelete = false;
            Groupnonitar.DirList = true;
            Groupnonitar.DirShowHidden = false;
            Groupnonitar.DirShowInList = true;
            Groupnonitar.DirShowReadOnly = false;
            Groupnonitar.FileAppend = false;
            Groupnonitar.FileDelete = false;
            Groupnonitar.FileDownload = false;
            Groupnonitar.FileRename = false;
            Groupnonitar.FileUpload = false;
            SFTPsite.SetPermission(Groupnonitar, false);
        }
    }
    public void SetPermissions(string folder, string group, string permissionsLevel)
    {
        folder = folder.Replace(@"//", "/");
        Permission perm = SFTPsite.GetBlankPermission(folder, group);
            perm.DirShowHidden = false;
            perm.DirShowReadOnly = false;

        if (permissionsLevel == "Modify")
        {
            perm.DirCreate = true;
            perm.DirDelete = true;
            perm.DirList = true;
            perm.DirShowInList = true;
            perm.FileAppend = true;
            perm.FileDelete = true;
            perm.FileDownload = true;
            perm.FileRename = true;
            perm.FileUpload = true;
            
        }
        else if (permissionsLevel == "Upload/Download")
        {
            perm.DirCreate = false;
            perm.DirDelete = false;
            perm.DirList = true;
            perm.DirShowInList = true;
            perm.FileAppend = false;
            perm.FileDelete = false;
            perm.FileDownload = true;
            perm.FileRename = false;
            perm.FileUpload = true;
        }
        else if (permissionsLevel == "Download")
        {
            
            perm.DirCreate = false;
            perm.DirDelete = false;
            perm.DirList = true;
            perm.DirShowInList = true;
            perm.FileAppend = false;
            perm.FileDelete = false;
            perm.FileDownload = true;
            perm.FileRename = false;
            perm.FileUpload = false;
           
        }
        else if (permissionsLevel == "Upload")
        {
            
            perm.DirCreate = false;
            perm.DirDelete = false;
            perm.DirList = true;
            perm.DirShowInList = true;
            perm.FileAppend = false;
            perm.FileDelete = false;
            perm.FileDownload = false;
            perm.FileRename = false;
            perm.FileUpload = true;    
        }
        SFTPsite.SetPermission(perm, false);
    }
}

