using ImageProcess2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using HNUDIP;
using AForge.Video.DirectShow;
using AForge.Video;

namespace Kisteria_DIP_activity
{
    public partial class Form1 : Form
    {
        Bitmap loaded, processed, subtracted;
        VideoCaptureDevice device;
        FilterInfoCollection filterInfoCollection;
        int mode;
        int col_mode = 0;
        int value;
        public Form1()
        {
            InitializeComponent();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            loaded = new Bitmap(openFileDialog1.FileName);
            pictureBox1.Image = loaded;
        }

        private void pixelCopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            processed = new Bitmap(loaded.Width, loaded.Height); //the size of the processed is now same with the loaded
            Color pixel;
            
            for(int x = 0; x < loaded.Width; x++)
                for(int y = 0; y < loaded.Height; y++)
                {
                    pixel = loaded.GetPixel(x, y);
                    processed.SetPixel(x, y, pixel);
                }
            pictureBox2.Image = processed;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            processed.Save(saveFileDialog1.FileName);
        }

        private void grayscalingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            processed = new Bitmap(loaded.Width, loaded.Height);
            BasicDIP.Greyscale(ref loaded, ref processed);
            pictureBox2.Image = processed;
        }

        private void inversionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            processed = new Bitmap(loaded.Width, loaded.Height);
            BasicDIP.Invert(ref loaded, ref processed);
            pictureBox2.Image = processed;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo filterinfo in filterInfoCollection)
            {
                comboBox1.Items.Add(filterinfo.Name);
            }
            comboBox1.SelectedIndex = 0;
            device = new VideoCaptureDevice();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void LoadBackground_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    pictureBox2.Image = new Bitmap(openFileDialog2.FileName);
                    processed = new Bitmap(openFileDialog2.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading image: " + ex.Message);
                }
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void openFileDialog2_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void SubtractBtn_Click(object sender, EventArgs e)
        {
            if (loaded == null || processed == null)
            {
                MessageBox.Show("Both loaded and processed images are required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (processed.Width != loaded.Width || processed.Height != loaded.Height)
            {
                Bitmap resizedProcessed = new Bitmap(loaded.Width, loaded.Height);
                using (Graphics g = Graphics.FromImage(resizedProcessed))
                {
                    g.DrawImage(processed, 0, 0, loaded.Width, loaded.Height);
                }
                processed = resizedProcessed;
            }

            // Threshold to determine if a pixel is "green enough"
            int greenThreshold = 50;
            subtracted = new Bitmap(loaded.Width, loaded.Height);

            // Lock bits for direct access without parallel processing
            BitmapData loadedData = loaded.LockBits(new Rectangle(0, 0, loaded.Width, loaded.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData processedData = processed.LockBits(new Rectangle(0, 0, processed.Width, processed.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData subtractedData = subtracted.LockBits(new Rectangle(0, 0, subtracted.Width, subtracted.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            // Get byte pointers
            IntPtr ptrLoaded = loadedData.Scan0;
            IntPtr ptrProcessed = processedData.Scan0;
            IntPtr ptrSubtracted = subtractedData.Scan0;

            // Allocate arrays for faster access
            int numBytes = Math.Abs(loadedData.Stride) * loaded.Height;
            byte[] loadedBytes = new byte[numBytes];
            byte[] processedBytes = new byte[numBytes];
            byte[] subtractedBytes = new byte[numBytes];

            // Copy data to byte arrays
            Marshal.Copy(ptrLoaded, loadedBytes, 0, numBytes);
            Marshal.Copy(ptrProcessed, processedBytes, 0, numBytes);

            // Sequential processing of pixels
            for (int j = 0; j < loaded.Height; j++)
            {
                for (int i = 0; i < loaded.Width; i++)
                {
                    int index = j * loadedData.Stride + i * 4;
                    byte blue = loadedBytes[index];
                    byte green = loadedBytes[index + 1];
                    byte red = loadedBytes[index + 2];

                    // Check if pixel is "green enough"
                    if (green > red + greenThreshold && green > blue + greenThreshold)
                    {
                        // Use processed pixel
                        subtractedBytes[index] = processedBytes[index];       // Blue
                        subtractedBytes[index + 1] = processedBytes[index + 1]; // Green
                        subtractedBytes[index + 2] = processedBytes[index + 2]; // Red
                        subtractedBytes[index + 3] = processedBytes[index + 3]; // Alpha
                    }
                    else
                    {
                        // Use original loaded pixel
                        subtractedBytes[index] = blue;
                        subtractedBytes[index + 1] = green;
                        subtractedBytes[index + 2] = red;
                        subtractedBytes[index + 3] = loadedBytes[index + 3]; // Alpha
                    }
                }
            }

            // Copy modified byte array back to bitmap
            Marshal.Copy(subtractedBytes, 0, ptrSubtracted, numBytes);

            // Unlock bits after processing
            loaded.UnlockBits(loadedData);
            processed.UnlockBits(processedData);
            subtracted.UnlockBits(subtractedData);

            // Display the result
            pictureBox3.Image = subtracted;
        }

        private void mirrorHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            processed = new Bitmap(loaded.Width, loaded.Height);
            BasicDIP.MirrorHorizontal(ref loaded, ref processed);
            pictureBox2.Image = processed;
        }

        private void mirrorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            processed = new Bitmap(loaded.Width, loaded.Height);
            BasicDIP.MirrorVertical(ref loaded, ref processed);
            pictureBox2.Image = processed;
        }

        private void sepiaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            processed = new Bitmap(loaded.Width, loaded.Height);
            BasicDIP.Sepia(ref loaded, ref processed);
            pictureBox2.Image = processed;
        }

        private void graToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = 1;
        }

        private void rotationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = 2;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (device.IsRunning == true)
            {
                if (mode == 2)
                {
                    value = trackBar1.Value;
                }
            }
            else
            {
                BasicDIP.Rotate(ref loaded, ref processed, trackBar1.Value);
                pictureBox2.Image = processed;
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            if (device.IsRunning == true)
            {
                if (mode == 8)
                {
                    value = trackBar3.Value;
                }
            }
            else
            {
                if (loaded == null)
                    return;

                Bitmap copy = (Bitmap)loaded.Clone();
                BitmapFilter.Contrast(copy, (SByte)trackBar3.Value);

                pictureBox2.Image = copy;
            }
        }

        private void trackBar5_Scroll(object sender, EventArgs e)
        {
            BasicDIP.Binary(ref loaded, ref processed, trackBar5.Value);
            pictureBox2.Image = processed;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            if (device.IsRunning == true)
            {
                if (mode == 3)
                {
                    value = trackBar2.Value;
                }
            }
            else
            {
                BasicDIP.Brightness(ref loaded, ref processed, trackBar2.Value);
                pictureBox2.Image = processed;
            }
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            BasicDIP.Scale(ref loaded, ref processed, trackBar4.Value);
            pictureBox2.Image = processed;
        }

        private void brightnessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = 3;
        }

        private void invertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = 4;
        }

        private void sepiaToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            mode = 5;
        }

        private void mirrorHorizontalToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            mode = 6;
        }

        private void mirrorVerticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = 7;
        }

        private void contrastToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = 8;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            device = new VideoCaptureDevice(filterInfoCollection[comboBox1.SelectedIndex].MonikerString);
            device.NewFrame += VideoCaptureDevice_NewFrame;
            device.Start();
        }

        private void VideoCaptureDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {

            Bitmap frame = (Bitmap)eventArgs.Frame.Clone();
            Bitmap filteredFrame = (Bitmap)frame.Clone();
            switch (mode)
            {
                case 0:

                    break;
                case 1:
                    BasicDIP.Greyscale(ref frame, ref filteredFrame);
                    break;
                case 2:
                    BasicDIP.Rotate(ref frame, ref filteredFrame, value);
                    break;
                case 3:
                    BasicDIP.Brightness(ref frame, ref filteredFrame, value);
                    break;
                case 4:
                    BasicDIP.Invert(ref frame, ref filteredFrame);
                    break;
                case 5:
                    BasicDIP.Sepia(ref frame, ref filteredFrame);
                    break;
                case 6:
                    BasicDIP.MirrorHorizontal(ref frame, ref filteredFrame);
                    break;
                case 7:
                    BasicDIP.MirrorVertical(ref frame, ref filteredFrame);
                    break;
                case 8:
                    Bitmap temp = new Bitmap(frame);
                    BasicDIP.Contrast(ref temp, (SByte)value);
                    filteredFrame = temp;
                    break;
            }
            if (mode != 0)
            {
                pictureBox2.Image = filteredFrame;
            }
            pictureBox1.Image = frame;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (device.IsRunning == true)
            {
                device.Stop();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (device.IsRunning == true)
            {
                device.Stop();
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {

        }

        private void histogramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BasicDIP.Histogram(ref loaded, ref processed);
            pictureBox2.Image = processed;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }
    }
}
