using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Product_model
{
    public partial class Form1 : Form
    {
        string filename = "model.txt";
        bool is_parsed = false;
        HashSet<string> my_facts = new HashSet<string>();
        HashSet<string> result = new HashSet<string>();
        //HashSet<string> found_films = new HashSet<string>();
        //HashSet<string> used_rules = new HashSet<string>();
        Dictionary<string, string> facts = new Dictionary<string, string>();
        Dictionary<string, string> films = new Dictionary<string, string>();
        Dictionary<string, Dictionary<string, List<string>>> rules = new Dictionary<string, Dictionary<string, List<string>>>();


        public Form1()
        {
            InitializeComponent();
        }

        private void button_forward_Click(object sender, EventArgs e)
        {
            result = new HashSet<string>();
            if (!is_parsed)
                parse(filename);
            foreach (CheckBox check in groupBox_facts.Controls.OfType<CheckBox>())
            {
                if (check.Checked)
                {
                    IEnumerable<char> fact_id = check.Name.ToString().TakeWhile(a => a != '_');
                    string fact_checked = "";
                    foreach (char a in fact_id)
                        fact_checked += a;
                    fact_checked = fact_checked.ToString().ToUpper();
                    my_facts.Add(fact_checked);
                }
            }
            forward();
        }

        private void forward()
        {
            int old_size = my_facts.Count;
            foreach (string rule_name in rules.Keys)
            {
                List<string> facts_for_rule = rules[rule_name].Values.First();
                string new_fact = rules[rule_name].Keys.First();
                string need_facts = "";
                foreach (string fact in facts_for_rule)
                {
                    need_facts += facts[fact] + ", ";
                    if (!my_facts.Contains(fact))
                        break;
                    if (fact == facts_for_rule.Last())
                        if (new_fact[0] == 'T')
                        {
                            string used_fact = rule_name + ": " + need_facts + " -> " + films[new_fact];
                            result.Add(used_fact);
                            result.Add(films[new_fact]);
                        }
                        else
                        {
                            string used_fact = rule_name + ": " + need_facts + " -> " + facts[new_fact];
                            result.Add(used_fact);
                            my_facts.Add(new_fact);
                        }
                }

            }
            if (my_facts.Count > old_size)
                forward();
            else
                write_result();
        }

        private void write_result()
        {
            label_result.Text = "";
            foreach (string film in result)
            {
                label_result.Text += film + "\n";
            }
        }

        private void button_backward_Click(object sender, EventArgs e)
        {
            result = new HashSet<string>();
            if (!is_parsed)
                parse(filename);
            foreach (CheckBox check in groupBox_facts.Controls.OfType<CheckBox>())
            {
                if (check.Checked)
                {
                    IEnumerable<char> fact_id = check.Name.ToString().TakeWhile(a => a != '_');
                    string fact_checked = "";
                    foreach (char a in fact_id)
                        fact_checked += a;
                    fact_checked = fact_checked.ToString().ToUpper();
                    my_facts.Add(fact_checked);
                }
            }
            backward();
        }

        private void backward()
        {
            label_result.Text = "";
            foreach (string film in films.Keys)
            {
                Dictionary<string, Dictionary<string, List<string>>> rule = new Dictionary<string, Dictionary<string, List<string>>>();
                Dictionary<string, string> fact = new Dictionary<string, string>();
                Dictionary<string, AndNode> and_dict = new Dictionary<string, AndNode>();
                Dictionary<string, OrNode> or_dict = new Dictionary<string, OrNode>();
                OrNode root = new OrNode(film);
                or_dict.Add(film, root);

                Stack<Node> tree = new Stack<Node>();
                tree.Push(root);
                while (tree.Count != 0)
                {
                    Node cur = tree.Pop();
                    if (cur is AndNode)
                    {
                        AndNode n = cur as AndNode;
                        foreach (string f in rules[n.rule].Values.First())
                            if (or_dict.ContainsKey(f))
                            {
                                cur.children.Add(or_dict[f]);
                                or_dict[f].parents.Add(cur);
                            }
                            else
                            {
                                or_dict.Add(f, new OrNode(f));
                                n.children.Add(or_dict[f]);
                                or_dict[f].parents.Add(n);
                                tree.Push(or_dict[f]);
                            }
                    }
                    if (cur is OrNode)
                    {
                        OrNode n = cur as OrNode;

                        foreach (string r in rules.Keys.Where(a => string.Equals(rules[a].Keys.First(), n.fact)))
                            if (and_dict.ContainsKey(r))
                            {
                                cur.children.Add(and_dict[r]);
                                and_dict[r].parents.Add(cur);
                            }
                            else
                            {
                                and_dict.Add(r, new AndNode(r));
                                n.children.Add(and_dict[r]);
                                and_dict[r].parents.Add(n);
                                tree.Push(and_dict[r]);
                            }
                    }
                }

                foreach (var val in or_dict)
                    if (my_facts.Contains(val.Key))
                    {
                        val.Value.flag = true;
                        foreach (Node p in val.Value.parents)
                            resolve(p);
                        if (root.flag == true)
                        {
                            label_result.Text += films[root.fact] + "\n";
                            break;
                        }
                    }
            }


        }

        private void resolve(Node n)
        {
            if (n.flag)
                return;
            if (n is AndNode)
                n.flag = n.children.All(c => c.flag == true);

            if (n is OrNode)
                n.flag = n.children.Any(c => c.flag == true);

            if (n.flag)
            {
                foreach (Node p in n.parents)
                    resolve(p);
            }
        }

        private void parse(string filename)
        {
            string name = "";
            is_parsed = true;
            string[] lines = File.ReadAllLines(filename);
            foreach (string line in lines)
            {
                string[] help;
                help = line.Split(' ', '\t');
                if (line[0] == 'F')
                {
                    name = "";
                    for (int i = 1; i < help.Count(); i++)
                        name += help[i] + " ";
                    facts.Add(help.First(), name);
                }
                else if (line[0] == 'S')
                {
                    name = "";
                    for (int i = 1; i < help.Count(); i++)
                        name += help[i] + " ";
                    facts.Add(help.First(), name);
                }
                else if (line[0] == 'T')
                {
                    name = "";
                    for (int i = 1; i < help.Count(); i++)
                        name += help[i] + " ";
                    films.Add(help.First(), name);
                }
                else if (line[0] == 'R')
                {
                    line.Trim(new Char[] { ',' });
                    int pos_of_line = -1;
                    List<string> help_facts = new List<string>();
                    foreach (string fact in help)
                    {
                        if (fact[0] == 'F')
                        {
                            fact.Trim(new Char[] { ',' });
                            help_facts.Add(fact);
                        }
                        else if (fact[0] == 'S')
                        {
                            fact.Trim(new Char[] { ',' });
                            if (pos_of_line != -1)
                            {
                                rules[help[0]] = new Dictionary<string, List<string>>() { { fact, help_facts } };
                            }
                            else
                            {
                                help_facts.Add(fact);
                            }
                        }
                        else if (fact[0] == 'T')
                        {
                            fact.Trim(new Char[] { ',' });
                            rules[help[0]] = new Dictionary<string, List<string>>() { { fact, help_facts } };
                        }
                        else if (fact == "->")
                        {
                            pos_of_line = 1;
                        }
                    }
                }
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void groupBox_facts_Enter(object sender, EventArgs e)
        {

        }
    }

    class Node
    {
        public List<Node> parents = new List<Node>();
        public List<Node> children = new List<Node>();
        public bool flag = false;

        public Node() { }

    }

    class AndNode : Node
    {
        public string rule = "";
        public AndNode()
        { }

        public AndNode(string rule)
        {
            this.rule = rule;
        }
    }

    class OrNode : Node
    {
        public string fact = "";
        public OrNode()
        { }

        public OrNode(string fact)
        {
            this.fact = fact;
        }
    }
}
