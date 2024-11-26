using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using OfficeOpenXml;
using System.Data.SqlClient;
using System.IdentityModel;

namespace fileexplorer
{
    public partial class Form1 : Form
    {
        private Dictionary<long, List<string>> sizeGroups = new Dictionary<long, List<string>>();
        private Dictionary<string, List<string>> fileHashes = new Dictionary<string, List<string>>();
        private Dictionary<string, long> folderSizes = new Dictionary<string, long>();
        private ToolStripMenuItem openLocationMenuItem;
        private ToolStripMenuItem deleteMenuItem;
        private ToolStripMenuItem copyMenuItem;
        private ToolStripMenuItem cutMenuItem;
        private string fileOperationPath;
        private string operationType;
        private ToolStripMenuItem pasteMenuItem;
        private ToolStripMenuItem propertiesMenuItem;


        public Form1()
        {
            InitializeComponent();
            InitializeDataGridView();
            InitializeChart();
            InitializeContextMenu();
            label1.Visible = false;
            progressBar1.Visible = false;
            // Hook up the CellMouseUp event
            dataGridView1.CellMouseUp += dataGridView1_CellMouseUp;
        }


        private void InitializeContextMenu()
        {
            contextMenuStrip2 = new ContextMenuStrip();

            // Create a menu item to open the location
            openLocationMenuItem = new ToolStripMenuItem("Open Location");
            openLocationMenuItem.Click += OpenLocationMenuItem_Click;
            contextMenuStrip2.Items.Add(openLocationMenuItem);

            // Create a delete menu item
            deleteMenuItem = new ToolStripMenuItem("Delete");
            deleteMenuItem.Click += deleteToolStripMenuItem_Click; // Attach the delete event handler
            contextMenuStrip2.Items.Add(deleteMenuItem); // Add to context menu

            // Copy menu item
            copyMenuItem = new ToolStripMenuItem("Copy");
            copyMenuItem.Click += CopyMenuItem_Click; // Attach event
            contextMenuStrip2.Items.Add(copyMenuItem);

            // Cut menu item
            cutMenuItem = new ToolStripMenuItem("Cut");
            cutMenuItem.Click += CutMenuItem_Click; // Attach event
            contextMenuStrip2.Items.Add(cutMenuItem);

            // Paste menu item
            pasteMenuItem = new ToolStripMenuItem("Paste");
            pasteMenuItem.Click += PasteMenuItem_Click; // Attach event
            contextMenuStrip2.Items.Add(pasteMenuItem);

            // Properties menu item
            propertiesMenuItem = new ToolStripMenuItem("Properties");
            propertiesMenuItem.Click += PropertiesMenuItem_Click; // Attach event handler
            contextMenuStrip2.Items.Add(propertiesMenuItem);


            // Hook up the opening event to dynamically modify the context menu
            contextMenuStrip2.Opening += ContextMenuStrip2_Opening;

            // Assign the context menu to the DataGridView
            dataGridView1.ContextMenuStrip = contextMenuStrip2;
        }


        private void CopyMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;

            fileOperationPath = dataGridView1.CurrentRow.Cells["Name"].Value.ToString();
            if (!string.IsNullOrEmpty(fileOperationPath) && File.Exists(fileOperationPath))
            {
                operationType = "Copy";
                MessageBox.Show($"File copied: {fileOperationPath}", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("The selected file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CutMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;

            fileOperationPath = dataGridView1.CurrentRow.Cells["Name"].Value.ToString();
            if (!string.IsNullOrEmpty(fileOperationPath) && File.Exists(fileOperationPath))
            {
                operationType = "Cut";
                MessageBox.Show($"File ready to cut: {fileOperationPath}", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("The selected file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void PasteMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(fileOperationPath) || string.IsNullOrEmpty(operationType))
            {
                MessageBox.Show("No file to paste.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dataGridView1.CurrentRow == null) return;

            string targetFolder = dataGridView1.CurrentRow.Cells["Name"].Value.ToString();
            if (!Directory.Exists(targetFolder))
            {
                MessageBox.Show("Please select a valid folder to paste into.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string targetPath = Path.Combine(targetFolder, Path.GetFileName(fileOperationPath));

            try
            {
                if (operationType == "Copy")
                {
                    File.Copy(fileOperationPath, targetPath, overwrite: true);
                    MessageBox.Show($"File copied to: {targetPath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (operationType == "Cut")
                {
                    File.Move(fileOperationPath, targetPath);
                    MessageBox.Show($"File moved to: {targetPath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // Clear the operation
                fileOperationPath = null;
                operationType = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during paste operation: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PropertiesMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;

            string path = dataGridView1.CurrentRow.Cells["Name"].Value.ToString();

            if (File.Exists(path))
            {
                ShowFileProperties(path);
            }
            else if (Directory.Exists(path))
            {
                ShowFolderProperties(path);
            }
            else
            {
                MessageBox.Show("The selected item does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ContextMenuStrip2_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;

            string path = dataGridView1.CurrentRow.Cells["Name"].Value.ToString();
            openLocationMenuItem.Enabled = Directory.Exists(path) || File.Exists(path);
            // Enable/disable Properties based on pending operation
            propertiesMenuItem.Enabled = File.Exists(path) || Directory.Exists(path);
            // Enable/disable Paste based on pending operation
            pasteMenuItem.Enabled = !string.IsNullOrEmpty(fileOperationPath) && !string.IsNullOrEmpty(operationType);
        }

        private void ShowFolderProperties(string folderPath)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(folderPath);

            string properties = $"Folder: {dirInfo.Name}\n" +
                                $"Path: {dirInfo.FullName}\n" +
                                $"Created: {dirInfo.CreationTime}\n" +
                                $"Attributes: {dirInfo.Attributes}";

            MessageBox.Show(properties, "Folder Properties", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowFileProperties(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            string properties = $"File: {fileInfo.Name}\n" +
                                $"Path: {fileInfo.FullName}\n" +
                                $"Size: {fileInfo.Length / 1024.0:F2} KB\n" +
                                $"Created: {fileInfo.CreationTime}\n" +
                                $"Last Modified: {fileInfo.LastWriteTime}\n" +
                                $"Attributes: {fileInfo.Attributes}";

            MessageBox.Show(properties, "File Properties", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }



        private void dataGridView1_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Check if a cell is clicked
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    // Select the clicked cell
                    dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];

                    // Show the context menu
                    contextMenuStrip2.Show(dataGridView1, dataGridView1.PointToClient(MousePosition));
                }
            }
        }

        private void OpenLocationMenuItem_Click(object sender, EventArgs e)
        {
            // Get the full path of the selected file or folder
            if (dataGridView1.CurrentRow != null)
            {
                string path = dataGridView1.CurrentRow.Cells["Name"].Value.ToString();

                if (Directory.Exists(path))
                {
                    // Open folder in File Explorer
                    System.Diagnostics.Process.Start("explorer.exe", path);
                }
                else if (File.Exists(path))
                {
                    // Open file in the default application
                    System.Diagnostics.Process.Start(path);
                }
                else
                {
                    MessageBox.Show("The selected path does not exist.");
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;

            string path = dataGridView1.CurrentRow.Cells["Name"].Value.ToString();

            try
            {
                // Check if it's a file or folder, then delete
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true); // true for recursive deletion
                }
                else
                {
                    MessageBox.Show("The specified file or folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Remove the row from DataGridView
                dataGridView1.Rows.Remove(dataGridView1.CurrentRow);
                MessageBox.Show("File/Folder deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while deleting the file/folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void InitializeDataGridView()
        {
            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("Name", "FullName");
            dataGridView1.Columns.Add("Size", "Size (MB)");
            dataGridView1.Columns.Add("Type", "Type");
            dataGridView1.Columns.Add("Note", "Remark");

            dataGridView1.Columns["Size"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            dataGridView1.Columns[0].Width = 365;
            dataGridView1.Columns[1].Width = 365;
            dataGridView1.Columns[2].Width = 365;
            dataGridView1.Columns[3].Width = 365;

            dataGridView1.CellFormatting += dataGridView1_CellFormatting;
        }

        private void InitializeChart()
        {
            chart1.Series.Clear();
            chart1.Series.Add("FolderSizes");
            chart1.Series["FolderSizes"].ChartType = SeriesChartType.Column;
            chart1.Series["FolderSizes"].IsValueShownAsLabel = true;
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Size" && e.Value != null)
            {
                if (long.TryParse(e.Value.ToString(), out long bytes))
                {
                    double megabytes = bytes / 1024f / 1024f;
                    e.Value = $"{megabytes:0.##} MB";
                    e.FormattingApplied = true;
                }
            }
        }

        private void buttonbrowsser_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folder = new FolderBrowserDialog();
            if (folder.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = folder.SelectedPath;
            }
            label1.Visible = false;
            progressBar1.Visible = false;
        }

        private void LoadFilesAndFolders(string path)
        {
            dataGridView1.Rows.Clear();
            sizeGroups.Clear();
            fileHashes.Clear();
            folderSizes.Clear();

            int totalFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length;
            int processedFiles = 0;

            progressBar1.Maximum = totalFiles;
            LoadFilesAndFoldersRecursive(path, ref processedFiles);
            progressBar1.Value = progressBar1.Maximum; // Ensure progress bar is complete
            label1.Text = "100%"; // Ensure final display of 100%
            label1.Visible = false;
            progressBar1.Visible = false;

            UpdateChart();
            UpdateScore();

        }

        private string ComputeFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "");
            }
        }


        private void LoadFilesAndFoldersRecursive(string path, ref int processedFiles, int level = 0)
        {


            long folderSize = 0;
            //level folder
            if (level > 8)
            {
                dataGridView1.Rows.Add(path, "", "Folder", "This folder is beyond the " + level + "-level depth limit.");
            }

            long parentFolderSize = CalculateFolderSize(path);
            string parentFormattedSize = FormatSize(parentFolderSize);


            // Helper method to calculate folder size
            long CalculateFolderSize(string folderPath)
            {
                long size = 0;

                try
                {
                    // Add file sizes
                    var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        size += fileInfo.Length;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    //Handle access denied exceptions(e.g., system folders or restricted access)
                }

                return size;
            }

            // Helper method to format size
            string FormatSize(long bytes)
            {
                string[] sizeUnits = { "B", "KB", "MB", "GB", "TB" };
                double size = bytes;
                int order = 0;

                while (size >= 1024 && order < sizeUnits.Length - 1)
                {
                    order++;
                    size /= 1024;
                }

                return $"{size:0.##} {sizeUnits[order]}";
            }
            try
            {
                var directories = Directory.GetDirectories(path);
                var files = Directory.GetFiles(path);


                // Check for the number of subfolders and files in the current folder
                const int maxfile = 10;
                const int maxfolder = 15;
                bool hasManySubfolders = directories.Length > maxfolder;
                bool hasManyFiles = files.Length > maxfile;
                string note = "";

                if (hasManySubfolders)
                {
                    note += "This folder contains more than " + maxfolder + " subfolders ";
                }
                if (hasManyFiles)
                {
                    note += (note == "" ? "" : "and ") + "more than " + maxfile + " files.";
                }

                foreach (var directory in directories)
                {
                    var dirInfo = new DirectoryInfo(directory);

                    // Skip if the folder is a system folder
                    if ((dirInfo.Attributes & FileAttributes.System) == FileAttributes.System)
                    {
                        continue;
                    }

                    // Check if this subfolder is empty
                    if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                    {
                        dataGridView1.Rows.Add(dirInfo.FullName, parentFormattedSize.ToString(), "Folder", "This folder is empty.");
                    }
                    else
                    {
                        dataGridView1.Rows.Add(dirInfo.FullName, parentFormattedSize.ToString(), "Folder", "");
                    }
                }

                //  note folder to the DataGridView row
                dataGridView1.Rows.Add(path, parentFormattedSize.ToString(), "Folder", note);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    long fileSize = fileInfo.Length;


                    // Accumulate folder size
                    folderSize += fileSize;
                    string extension = fileInfo.Extension.ToLower();
                    string fileNote = "";

                    //file is old (more than 9.75 years)
                    double oldFileDays = 9.75 * 365;
                    DateTime oldFileThreshold = DateTime.Now.AddDays(-oldFileDays);
                    if (fileInfo.LastWriteTime < oldFileThreshold)
                    {
                        fileNote = "This file is old. Created at " + fileInfo.LastWriteTime;
                    }

                    //file > 5 GB
                    int filegb = 5;
                    long limitsize = filegb * 1024 * 1024 * 1024L;
                    if (fileSize > limitsize)
                    {
                        fileNote = "This file exceeds " + filegb + "GB.";
                    }

                    //duplicate file
                    string hash = ComputeFileHash(file);

                    if (fileHashes.ContainsKey(hash))
                    {
                        fileNote += (fileNote == "" ? "" : " ") + "This file is a duplicate.";
                        fileHashes[hash].Add(file);
                    }
                    else
                    {
                        fileHashes[hash] = new List<string> { file };
                    }

                    dataGridView1.Rows.Add(fileInfo.FullName, fileSize.ToString(), extension, fileNote);
                    processedFiles++;
                    progressBar1.Value = Math.Min(processedFiles, progressBar1.Maximum);
                    label1.Text = $"{(int)((processedFiles / (double)progressBar1.Maximum) * 100)}%";
                    Application.DoEvents();
                }

                //  accumulated size for this folder
                folderSizes[path] = folderSize;

                foreach (var directory in directories)
                {
                    // Recursively load subdirectories
                    LoadFilesAndFoldersRecursive(directory, ref processedFiles, level + 1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}");
            }
        }

        private void UpdateChart()
        {

            chart1.Series["FolderSizes"].ToolTip = "true";
            Series series = chart1.Series["FolderSizes"];
            series.ChartType = SeriesChartType.Column;  // Change chart type to column
            series.IsValueShownAsLabel = true;  // Show values on each column

            // Set properties for better readability in a bar chart
            chart1.ChartAreas[0].AxisX.LabelStyle.Angle = 90;
            chart1.ChartAreas[0].AxisX.Interval = 1;
            chart1.ChartAreas[0].AxisY.Title = "Size (MB)";

            foreach (var folder in folderSizes)
            {
                // Convert folder size from bytes to megabytes (MB)
                double sizeInMB = folder.Value / (1024.0 * 1024.0);

                // Short label to avoid overlap if the folder name is too long
                string shortLabel = folder.Key.Length > 15 ? folder.Key.Substring(0, 12) + "..."
                    : folder.Key;

                // Data point with short label, size value, and tooltip for the full name
                DataPoint point = new DataPoint
                {
                    AxisLabel = shortLabel,
                    YValues = new[] { sizeInMB },
                    // Display the size on the bar
                    Label = $"{sizeInMB:0.##} MB",
                    // Tooltip to display full folder name
                    LabelToolTip = $"{folder.Key} - {sizeInMB:0.##} MB"
                };

                series.Points.Add(point);
            }
        }

        private void showbtn_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            progressBar1.Visible = true;
            label1.Visible = true;
            string selectedPath = textBox1.Text;

            if (Directory.Exists(selectedPath))
            {
              
                    LoadFilesAndFolders(selectedPath);
                
            }
            else
            {
                MessageBox.Show("Please select a valid directory.");
            }

        }


        //profile
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // get variable environment USERPROFILE
                string userProfile = Environment.GetEnvironmentVariable("USERPROFILE");

                if (!string.IsNullOrEmpty(userProfile))
                {
                    // show USERPROFILE at textBox1
                    textBox1.Text = userProfile;
                }
                else
                {
                    textBox1.Text = "USERPROFILE not found.";
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // handling denied access 
                MessageBox.Show("Access denied to the path. " + ex.Message);
            }
            catch (Exception ex)
            {
                // handling other error
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }

        //save to excel
        private void button2_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "Excel Files|*.xlsx" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (ExcelPackage package = new ExcelPackage())
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Sheet1");

                        // Add column headers
                        for (int i = 0; i < dataGridView1.Columns.Count; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = dataGridView1.Columns[i].HeaderText;
                        }

                        // Add rows
                        for (int i = 0; i < dataGridView1.Rows.Count; i++)
                        {
                            for (int j = 0; j < dataGridView1.Columns.Count; j++)
                            {
                                worksheet.Cells[i + 2, j + 1].Value = dataGridView1.Rows[i].Cells[j].Value;
                            }
                        }

                        // Save to file
                        File.WriteAllBytes(sfd.FileName, package.GetAsByteArray());
                        MessageBox.Show("Exported successfully!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
        // score
        private void UpdateScore()
        {
            int score = 0;
            int duplicateFileCount = 0;
            int oldFileCount = 0;
            int level = 0;
            int maxfolder = 0;
            int maxfile = 0;
            int empty = 0;
            int size = 0;

            // Iterate through each row in the DataGridView
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue; // Skip the new row placeholder

                // Retrieve the "Note" cell value
                string note = row.Cells["Note"].Value?.ToString()?.ToLower();

                // Debugging: Handle null or empty "Note" values
                if (note.Contains("empty"))
                {
                    empty++; // Increment empty count for null/empty notes
                    score += 1; // Deduct points for empty notes

                }

                // Scoring conditions
                if (note.Contains("duplicate"))
                {
                    score += 1; // Deduct points for duplicates
                    duplicateFileCount++;
                }

                if (note.Contains("old"))
                {
                    score += 1; // Deduct points for old files
                    oldFileCount++;
                }

                if (note.Contains("level"))
                {
                    level++; // Increment level count
                    score += 1; // Add points for level
                }

                if (note.Contains("maxfolder"))
                {
                    maxfolder++; // Increment maxfolder count
                    score += 1; // Add points for maxfolder
                }

                if (note.Contains("maxfile"))
                {
                    maxfile++; // Increment maxfile count
                    score += 1; // Add points for maxfile
                }

                if (note.Contains("size"))
                {
                    size++; // Increment size count
                    score += 1; // Add points for size
                }
            }

            // Ensure the score doesn't drop below 0
            score = Math.Max(score, 0);

            // Update the score label with category breakdown
            label4.Text = $"Score: -{score}\n";
            //SaveScoreToDatabase(score);

        }

     
        //save to db

        //
        //    private void SaveScoreToDatabase(int Score)
        //    {
        //        string connectionString = "Data Source=ASUS;Initial Catalog=FiveS;Integrated Security=True;Encrypt=False;TrustServerCertificate=True";
        //        string Name = NamatextBox.Text;
        //        string Path = textBox1.Text;
        //        using (SqlConnection connection = new SqlConnection(connectionString))
        //        {
        //            string query = @"INSERT INTO Scoring (Name,Score,Path) VALUES (@Name,@Score,@Path)";

        //            using (SqlCommand command = new SqlCommand(query, connection))
        //            {
        //                // Add parameters to prevent SQL injection

        //                command.Parameters.AddWithValue("@Score", Score);
        //                command.Parameters.AddWithValue("@Name", Name);
        //                command.Parameters.AddWithValue("@Path", Path);

        //                connection.Open();
        //                command.ExecuteNonQuery();
        //            }
        //        }
        //    }
        //}
    }
}

