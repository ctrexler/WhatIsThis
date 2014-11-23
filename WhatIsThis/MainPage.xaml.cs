
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
 
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641
 
namespace WhatIsThis
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MediaCapture mediaCapture = new MediaCapture();
        private string connString = "DefaultEndpointsProtocol=https;AccountName=whatisthis;AccountKey=FY9iE5LD19XI4YxdzvPjHE/CXZBjHBnZH30wZM48qtYgQePaI7i3pNwkJFVJuDZeB1k1rIeLwXpRbSUYadajIw==";

        public MainPage()
        {
            this.InitializeComponent();

            Initialize();
 
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }
 
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.
 
            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        async public void Initialize()
        {
            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();

            PreviewElement.Source = mediaCapture;
            await mediaCapture.StartPreviewAsync();
        }

        async private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            PreviewElement.Visibility = Visibility.Collapsed;

            //declare image format
            ImageEncodingProperties format = ImageEncodingProperties.CreateJpeg();

            //generate file in local folder:
            StorageFile capturefile = await ApplicationData.Current.LocalFolder.CreateFileAsync("photo_" + DateTime.Now.Ticks.ToString(), CreationCollisionOption.ReplaceExisting);

            ////take & save photo
            await mediaCapture.CapturePhotoToStorageFileAsync(format, capturefile);

            //show captured photo
            BitmapImage img = new BitmapImage(new Uri(capturefile.Path));
            takenImage.Source = img;
            takenImage.Visibility = Visibility.Visible;
            await UploadToAzureStorage(capturefile);
        }

        private async Task<int> UploadToAzureStorage(StorageFile image)
        {

            try
            {
                //  create Azure Storage
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connString);

                //  create a blob client.
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                //  create a container 
                CloudBlobContainer container = blobClient.GetContainerReference("blob");

                //  create a block blob
                CloudBlockBlob blockBlob = container.GetBlockBlobReference("filename");

                //  create a local file
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("filename", CreationCollisionOption.ReplaceExisting);

                var firstPicture = image;

                var memoryStream = new MemoryStream();
                var stream = await firstPicture.OpenStreamForReadAsync();
                var b = stream.ReadByte();
                while (b != -1)
                {
                    memoryStream.WriteByte(Convert.ToByte(b));
                    b = stream.ReadByte();
                }


                Stream fileStream = null;
                //using (memoryStream)
                using (fileStream = await file.OpenStreamForWriteAsync())
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    await memoryStream.CopyToAsync(fileStream);
                    await memoryStream.FlushAsync();
                }

                fileStream = await file.OpenStreamForReadAsync();


                //  upload to Azure Storage 
                await blockBlob.UploadFromStreamAsync(fileStream.AsInputStream(), fileStream.Length);

                return 1;
            }
            catch
            {
                //  return error
                return 0;
            }
        }
    }
}
 