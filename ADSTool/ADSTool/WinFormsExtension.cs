using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.ListView;

namespace ADSTool
{

    public static class WinFromsExtension
    {
        public static bool DialogOK(this CommonDialog dialog)
        {
            return dialog.ShowDialog() == DialogResult.OK;
        }

        public static void AddItem(this ListViewItemCollection collection, params string[] strs)
        {
            collection.Add(new ListViewItem(strs));
        }

        public static bool FileExists(params string[] files)
        {
            return files.All(File.Exists);
        }

        public static void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static bool ItemSelected(this ListView listView)
        {
            return listView.SelectedIndices.Count != 0;
        }

        public static ListViewItem GetSelectedItem(this ListView listView)
        {
            return listView.SelectedItems[0];
        }
    }
}
