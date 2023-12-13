using DSIO.Filters.Api.Sdk.Types.V1;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace WpfSample
{
    /// <summary>
    /// Interaction logic for ImageDataAndInfo.xaml
    /// </summary>
    public partial class ImageDataAndInfo : Window
    {
        public ImageDataAndInfo(MainViewModel mainViewModel)
        {
            DataContext = mainViewModel;
            InitializeComponent();

            // Initialize text box with sample ImageInfo Json data
            TbxImageInfo.Text = @"{
    'acquisitionInfo':
    {
        'binning': 'Unbinned'
    },
    'lutInfo':
    {
        'gamma': 2.3,
        'slope': 65535,
        'offset': 0,
        'totalGrays': 4096,
        'minimumGray': 3612,
        'maximumGray': 418
    }
}";

        }

        public MainViewModel ViewModel => DataContext as MainViewModel;

        private void BtnBrowseFileOpen_OnClick(object sender, RoutedEventArgs e)
        {
            // Use a FileDialog browser to allow user to select a PNG file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png)|*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                // Extract the full path of image file
                var fileName = openFileDialog.FileName;

                // If the image is 8-bit, then we need to scale it up to 12
                if (Is8BitPng(fileName))
                {
                    // Determine a new filename to hold the upscaled image
                    var imageScaledPath = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(fileName),
                        $"{openFileDialog.SafeFileName}-scaled.png");

                    // Upscale the image
                    if (Upscale8bitImage(fileName, imageScaledPath))
                    {
                        // If successful, we want to use the scaled image file instead of the selected file
                        fileName = imageScaledPath;
                    }
                }

                // Update the Viewmodel with path to image for upload
                ViewModel.UploadImageFileName = fileName;
            }
        }

        private void BtnSubmit_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ViewModel.UploadImageFileName))
            {
                MessageBox.Show("Please select a image file to upload", "Image Upload");
                return;
            }

            try
            {
                // Convert the Json data to an ImageInfo
                // The sample allows the developer to enter Json data, but we need an ImageInfo
                // object to use with the SDK. So we try to deserialize the supplied Json now
                var imageInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<ImageInfo>(TbxImageInfo.Text);
                ViewModel.UploadImageInfo = imageInfo;

                this.DialogResult = true;
                this.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Json Deserialize");
            }
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            // Reset Upload parameters on cancel
            ViewModel.UploadImageFileName = null;
            ViewModel.UploadImageInfo = null;

            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        ///  Checks if an image is an 8-bit gray scale image
        /// </summary>
        /// <param name="filename">Full path of the image file</param>
        /// <returns>true if image is 8-bit grayscale</returns>
        private bool Is8BitPng(string filename)
        {
            // Create a BitmapSource representing the 8-bit input image
            var decoder = BitmapDecoder.Create(new Uri(filename), 
                BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
           return decoder.Frames[0].Format == PixelFormats.Gray8;
        }

        /// <summary>
        /// Scales an image from 8 to 12 bits and saves the resulting image to indicated file.
        /// </summary>
        /// <param name="imageFileNameIn"></param>
        /// <param name="imageFileNameOut"></param>
        /// <returns>true if the image was upscaled successfully</returns>
        private bool Upscale8bitImage(string imageFileNameIn, string imageFileNameOut)
        {
            // Create a decoder to read the input image file
            BitmapDecoder decoder = BitmapDecoder.Create(new Uri(imageFileNameIn), 
                BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0];

            // Make sure the input source is Gray8
            if (bitmapSource.Format != PixelFormats.Gray8)
                return false;

            // Extract dimensions of the input image
            int width = bitmapSource.PixelWidth;
            int height = bitmapSource.PixelHeight;

            // Copy the 8-bit image pixel data to an array
            int stride = width * (bitmapSource.Format.BitsPerPixel / 8);
            byte[] pixel8 = new byte[width * height];
            bitmapSource.CopyPixels(pixel8, stride, 0);

            // Allocate a new pixel array to hold the 12-bit data
            ushort[] pixel12 = new ushort[width * height];

            // Multiply each pixel value by a factor of 16 (convert from 8-bit to 12-bit)
            for (int i = 0; i < pixel12.Length; i++)
            {
                pixel12[i] = (ushort)(pixel8[i] << 4);
            }

            // Create a BitmapSource using the new 12-bit data
            int stride12 = width*2;
            var bitmapSourceOut = BitmapSource.Create(width, height, bitmapSource.DpiX, bitmapSource.DpiY,
                PixelFormats.Gray16, null, pixel12, stride12);

            // Create a PNG bitmap encoder to save the result
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSourceOut));

            // Save the new 16-bit grayscale image
            using (FileStream fileStream = new FileStream(imageFileNameOut, FileMode.Create))
            {
                encoder.Save(fileStream);
            }

            return true;
        }
    }
}
