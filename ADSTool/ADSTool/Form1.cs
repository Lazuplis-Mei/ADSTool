using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ADSTool.AlternateDataStream;
using static ADSTool.WinFromsExtension;
using static Microsoft.VisualBasic.Interaction;


namespace ADSTool
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private string fileName;
        private readonly Random random = new Random();

        private void FlushListView()
        {
            listView1.Items.Clear();
            foreach(var item in GetStreamDatas(fileName))
            {
                string text;
                if (item.Length >= 1024 * 1024)
                {
                    text = (item.Length / (1024f * 1024)).ToString("0.00") + " MB";
                }
                else if (item.Length >= 1024)
                {
                    text = (item.Length / 1024f).ToString("0.00") + " KB";
                }
                else
                {
                    text = item.Length.ToString() + " Bytes";
                }

                listView1.Items.AddItem(item.Stream, text);
            }
            listView1.Items[0].ForeColor = Color.Gray;
            listView1.Items[0].BackColor = Color.FromArgb(0xFF, 0xE5, 0xE5, 0xE5);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(ofdFile.DialogOK())
            {
                fileName = ofdFile.FileName;
                textBox1.Text = fileName;
                FlushListView();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(ofdDataFile.DialogOK())
            {
                textBox2.Text = ofdDataFile.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(FileExists(fileName, textBox2.Text))
            {
                string streamName = InputBox("数据流名称", "输入流名称", 
                    "DATA" + random.Next(100000, 1000000));
                if(string.IsNullOrWhiteSpace(streamName))
                {
                    ShowError("流名称不能为空");
                }
                else if(ExistsStream(fileName,streamName))
                {
                    ShowError("指定的流名称已存在");
                }
                else
                {
                    WriteStreamFromFile(fileName, streamName, textBox2.Text);
                    FlushListView();
                }
            }
        }

        private void 另存为文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(listView1.ItemSelected() && sfdFile.DialogOK())
            {
                string streamName = listView1.GetSelectedItem().Text;
                SaveStreamToFile(fileName, streamName, sfdFile.FileName);
            }
        }

        private void 删除流数据ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(listView1.ItemSelected() && listView1.SelectedIndices[0] != 0)
            {
                DeleteStream(fileName, listView1.GetSelectedItem().Text);
                FlushListView();
            }
            
        }

        private void 重命名ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(listView1.ItemSelected() && listView1.SelectedIndices[0] != 0)
            {
                string streamName = InputBox("数据流名称", "输入流名称",
                    listView1.GetSelectedItem().Text);
                if(string.IsNullOrWhiteSpace(streamName))
                {
                    ShowError("流名称不能为空");
                }
                else if(ExistsStream(fileName, streamName))
                {
                    ShowError("指定的流名称已存在");
                }
                else
                {
                    using(var stream = CreateStream(fileName, streamName))
                    {
                        streamName = listView1.GetSelectedItem().Text;
                        CopyToStream(fileName, streamName, stream);
                        DeleteStream(fileName, streamName);
                    }
                    FlushListView();
                }
            }
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if(files.Length >= 1 && File.Exists(files[0]))
            {
                fileName = files[0];
                textBox1.Text = fileName;
                FlushListView();
            }
        }

        private void textBox2_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if(files.Length >= 1 && File.Exists(files[0]))
            {
                textBox2.Text = files[0];
            }
        }
    }

}
