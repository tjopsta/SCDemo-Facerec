using System;
using System.Runtime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.IO;
using Windows.Devices.Gpio;


namespace TSPFaceRecRasPi
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaCapture mediaCapture;
        private StorageFile photoFile;
        private StorageFile recordStorageFile;
        private StorageFile audioFile;
        private string PHOTO_FILE_NAME = "photo.jpg";
        private readonly string VIDEO_FILE_NAME = "video.mp4";
        private readonly string AUDIO_FILE_NAME = "audio.mp3";
        private bool isPreviewing;
        private bool isRecording;

        private const int LED_PIN = 31;
        private const int BUTTON_PIN = 29;
        private GpioPin ledPin;
        private GpioPin buttonPin;
        private GpioPinValue ledPinValue = GpioPinValue.High;
        


        #region HELPER_FUNCTIONS

        enum Action
        {
            ENABLE,
            DISABLE
        }
        /// <summary>
        /// Helper function to enable or disable Initialization buttons
        /// </summary>
        /// <param name="action">enum Action</param>
        private void SetInitButtonVisibility(Action action)
        {
            if (action == Action.ENABLE)
            {
                
                audio_init.IsEnabled = true;
            }
            else
            {
                
                audio_init.IsEnabled = false;
            }
        }

        /// <summary>
        /// Helper function to enable or disable video related buttons (TakePhoto, Start Video Record)
        /// </summary>
        /// <param name="action">enum Action</param>
        private void SetVideoButtonVisibility(Action action)
        {
            if (action == Action.ENABLE)
            {
                takePhoto.IsEnabled = true;
                takePhoto.Visibility = Visibility.Visible;

                recordVideo.IsEnabled = true;
                recordVideo.Visibility = Visibility.Visible;
            }
            else
            {
                takePhoto.IsEnabled = false;
                takePhoto.Visibility = Visibility.Collapsed;

                recordVideo.IsEnabled = false;
                recordVideo.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Helper function to enable or disable audio related buttons (Start Audio Record)
        /// </summary>
        /// <param name="action">enum Action</param>
        private void SetAudioButtonVisibility(Action action)
        {
            if (action == Action.ENABLE)
            {
                recordAudio.IsEnabled = true;
                recordAudio.Visibility = Visibility.Visible;
            }
            else
            {
                recordAudio.IsEnabled = false;
                recordAudio.Visibility = Visibility.Collapsed;
            }
        }
        #endregion
        public MainPage()
        {
            this.InitializeComponent();

            SetInitButtonVisibility(Action.ENABLE);
            SetVideoButtonVisibility(Action.DISABLE);
            SetAudioButtonVisibility(Action.DISABLE);

            isRecording = false;
            isPreviewing = false;

            InitVideo();
            //InitGPIO();

        }


        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                ///*GpioStatus.Text = "There is n*/o GPIO controller on this device.";
                return;
            }

            buttonPin = gpio.OpenPin(BUTTON_PIN);
            ledPin = gpio.OpenPin(LED_PIN);

            // Initialize LED to the OFF state by first writing a HIGH value
            // We write HIGH because the LED is wired in a active LOW configuration
            ledPin.Write(GpioPinValue.High);
            ledPin.SetDriveMode(GpioPinDriveMode.Output);

            // Check if input pull-up resistors are supported
            if (buttonPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
                buttonPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            else
                buttonPin.SetDriveMode(GpioPinDriveMode.Input);

            // Set a debounce timeout to filter out switch bounce noise from a button press
            buttonPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);

            // Register for the ValueChanged event so our buttonPin_ValueChanged 
            // function is called when the button is pressed
            buttonPin.ValueChanged += buttonPin_ValueChanged;

            //GpioStatus.Text = "GPIO pins initialized correctly.";
        }

        private void buttonPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            // toggle the state of the LED every time the button is pressed
            if (e.Edge == GpioPinEdge.FallingEdge)
            {
                ledPinValue = (ledPinValue == GpioPinValue.Low) ?
                    GpioPinValue.High : GpioPinValue.Low;
                ledPin.Write(ledPinValue);
            }

            
        }

        /// <summary>
        /// 'Initialize Audio and Video' button action function
        /// Dispose existing MediaCapture object and set it up for audio and video
        /// Enable or disable appropriate buttons
        /// - DISABLE 'Initialize Audio and Video' 
        /// - DISABLE 'Start Audio Record'
        /// - ENABLE 'Initialize Audio Only'
        /// - ENABLE 'Start Video Record'
        /// - ENABLE 'Take Photo'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void InitVideo()
        {
            // Disable all buttons until initialization completes

            SetInitButtonVisibility(Action.DISABLE);
            SetVideoButtonVisibility(Action.DISABLE);
            SetAudioButtonVisibility(Action.DISABLE);

            try
            {
                if (mediaCapture != null)
                {
                    // Cleanup MediaCapture object
                    if (isPreviewing)
                    {
                        await mediaCapture.StopPreviewAsync();
                        captureImage.Source = null;
                        playbackElement.Source = null;
                        isPreviewing = false;
                    }
                    if (isRecording)
                    {
                        await mediaCapture.StopRecordAsync();
                        isRecording = false;
                        recordVideo.Content = "Start Video Record";
                        recordAudio.Content = "Start Audio Record";
                    }
                    mediaCapture.Dispose();
                    mediaCapture = null;
                }

                status.Text = "Initializing camera to capture audio and video...";
                // Use default initialization
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();

                // Set callbacks for failure and recording limit exceeded
                status.Text = "Device successfully initialized for video recording!";
                mediaCapture.Failed += new MediaCaptureFailedEventHandler(mediaCapture_Failed);
                mediaCapture.RecordLimitationExceeded += new Windows.Media.Capture.RecordLimitationExceededEventHandler(mediaCapture_RecordLimitExceeded);

                // Start Preview                
                previewElement.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
                status.Text = "Camera preview succeeded";

                // Enable buttons for video and photo capture
                SetVideoButtonVisibility(Action.ENABLE);

                // Enable Audio Only Init button, leave the video init button disabled
                audio_init.IsEnabled = true;
            }
            catch (Exception ex)
            {
                status.Text = "Unable to initialize camera for audio/video mode: " + ex.Message;
            }


        }
        
        private async void Cleanup()
        {
            if (mediaCapture != null)
            {
                // Cleanup MediaCapture object
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                    captureImage.Source = null;
                    playbackElement.Source = null;
                    isPreviewing = false;
                }
                if (isRecording)
                {
                    await mediaCapture.StopRecordAsync();
                    isRecording = false;
                    recordVideo.Content = "Start Video Record";
                    recordAudio.Content = "Start Audio Record";
                }
                mediaCapture.Dispose();
                mediaCapture = null;
            }
            SetInitButtonVisibility(Action.ENABLE);
        }

        private async void TakePicture()
        {
        try
            {
                takePhoto.IsEnabled = false;
                recordVideo.IsEnabled = false;
                captureImage.Source = null;


                //new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation().Id.ToString()

                PHOTO_FILE_NAME = "YYC01_" 
                    + DateTime.UtcNow.Year + DateTime.UtcNow.Month + DateTime.UtcNow.Day + DateTime.UtcNow.Hour + DateTime.UtcNow.Minute 
                    + DateTime.UtcNow.Second + ".jpg";


                photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                    PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);
                takePhoto.IsEnabled = true;
                status.Text = "Take Photo succeeded and file saved to: " + photoFile.Path;

                IRandomAccessStream photoStream = await photoFile.OpenReadAsync();
                BitmapImage bitmap = new BitmapImage();
                bitmap.SetSource(photoStream);
                captureImage.Source = bitmap;
                //---------------------------------------------------------------------------
                status.Text = "Uploading Photo to IOT Hub";

                var reader = new DataReader(photoStream.GetInputStreamAt(0));
                var bytes = new byte[photoStream.Size];
                await reader.LoadAsync((uint)photoStream.Size);
                reader.ReadBytes(bytes);
                var stream = new MemoryStream(bytes);

                await AzureIoTHub.SendToBlobAsyncStream(photoFile, stream);

          
                status.Text = "File Uploaded!";
            }


            
            catch (Exception ex)
            {
                status.Text = ex.Message;
                Cleanup();
            }
            finally
            {
                takePhoto.IsEnabled = true;
                recordVideo.IsEnabled = true;
            }
        }


        private void cleanup_Click(object sender, RoutedEventArgs e)
        {
            SetInitButtonVisibility(Action.DISABLE);
            SetVideoButtonVisibility(Action.DISABLE);
            SetAudioButtonVisibility(Action.DISABLE);
            Cleanup();
        }


        /// <summary>
        /// 'Initialize Audio Only' button action function
        /// Dispose existing MediaCapture object and set it up for audio only
        /// Enable or disable appropriate buttons
        /// - DISABLE 'Initialize Audio Only' 
        /// - DISABLE 'Start Video Record'
        /// - DISABLE 'Take Photo'
        /// - ENABLE 'Initialize Audio and Video'
        /// - ENABLE 'Start Audio Record'        
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void initAudioOnly_Click(object sender, RoutedEventArgs e)
        {
            // Disable all buttons until initialization completes
            SetInitButtonVisibility(Action.DISABLE);
            SetVideoButtonVisibility(Action.DISABLE);
            SetAudioButtonVisibility(Action.DISABLE);

            try
            {
                if (mediaCapture != null)
                {
                    // Cleanup MediaCapture object
                    if (isPreviewing)
                    {
                        await mediaCapture.StopPreviewAsync();
                        captureImage.Source = null;
                        playbackElement.Source = null;
                        isPreviewing = false;
                    }
                    if (isRecording)
                    {
                        await mediaCapture.StopRecordAsync();
                        isRecording = false;
                        recordVideo.Content = "Start Video Record";
                        recordAudio.Content = "Start Audio Record";
                    }
                    mediaCapture.Dispose();
                    mediaCapture = null;
                }

                status.Text = "Initializing camera to capture audio only...";
                mediaCapture = new MediaCapture();
                var settings = new Windows.Media.Capture.MediaCaptureInitializationSettings();
                settings.StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.Audio;
                settings.MediaCategory = Windows.Media.Capture.MediaCategory.Other;
                settings.AudioProcessing = Windows.Media.AudioProcessing.Default;
                await mediaCapture.InitializeAsync(settings);

                // Set callbacks for failure and recording limit exceeded
                status.Text = "Device successfully initialized for audio recording!" + "\nPress \'Start Audio Record\' to record";
                mediaCapture.Failed += new MediaCaptureFailedEventHandler(mediaCapture_Failed);
                mediaCapture.RecordLimitationExceeded += new Windows.Media.Capture.RecordLimitationExceededEventHandler(mediaCapture_RecordLimitExceeded);

                // Enable buttons for audio
                SetAudioButtonVisibility(Action.ENABLE);

                // Enable Audio and video Only Init button
           
            }
            catch (Exception ex)
            {
                status.Text = "Unable to initialize camera for audio mode: " + ex.Message;
            }
        }

        /// <summary>
        /// 'Take Photo' button click action function
        /// Capture image to a file in the default account photos folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void takePhoto_Click(object sender, RoutedEventArgs e)
        {
            TakePicture();
        }


        public async Task UploadFile(Windows.Storage.StorageFile Photofile)
        {
            var sourceData = new FileStream(@photoFile.Path, FileMode.Open);
            await sourceData.ReadAsync(new byte[1], 0, 1);
            AzureIoTHub.SendToBlobAsyncStream(photoFile, sourceData).Wait();
        }




        /// <summary>
        /// 'Start Video Record' button click action function
        /// Button name is changed to 'Stop Video Record' once recording is started
        /// Records video to a file in the default account videos folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void recordVideo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                takePhoto.IsEnabled = false;
                recordVideo.IsEnabled = false;
                playbackElement.Source = null;

                if (recordVideo.Content.ToString() == "Start Video Record")
                {
                    takePhoto.IsEnabled = false;
                    status.Text = "Initialize video recording";
                    String fileName;
                    fileName = VIDEO_FILE_NAME;

                    recordStorageFile = await Windows.Storage.KnownFolders.VideosLibrary.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.GenerateUniqueName);

                    status.Text = "Video storage file preparation successful";

                    MediaEncodingProfile recordProfile = null;
                    recordProfile = MediaEncodingProfile.CreateMp4(Windows.Media.MediaProperties.VideoEncodingQuality.Auto);

                    await mediaCapture.StartRecordToStorageFileAsync(recordProfile, recordStorageFile);
                    recordVideo.IsEnabled = true;
                    recordVideo.Content = "Stop Video Record";
                    isRecording = true;
                    status.Text = "Video recording in progress... press \'Stop Video Record\' to stop";
                }
                else
                {
                    takePhoto.IsEnabled = true;
                    status.Text = "Stopping video recording...";
                    await mediaCapture.StopRecordAsync();
                    isRecording = false;

                    var stream = await recordStorageFile.OpenReadAsync();
                    playbackElement.AutoPlay = true;
                    playbackElement.SetSource(stream, recordStorageFile.FileType);
                    playbackElement.Play();
                    status.Text = "Playing recorded video" + recordStorageFile.Path;
                    recordVideo.Content = "Start Video Record";
                }
            }
            catch (Exception ex)
            {
                if (ex is System.UnauthorizedAccessException)
                {
                    status.Text = "Unable to play recorded video; video recorded successfully to: " + recordStorageFile.Path;
                    recordVideo.Content = "Start Video Record";
                }
                else
                {
                    status.Text = ex.Message;
                    Cleanup();
                }
            }
            finally
            {
                recordVideo.IsEnabled = true;
            }
        }

        /// <summary>
        /// 'Start Audio Record' button click action function
        /// Button name is changes to 'Stop Audio Record' once recording is started
        /// Records audio to a file in the default account video folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void recordAudio_Click(object sender, RoutedEventArgs e)
        {
            recordAudio.IsEnabled = false;
            playbackElement3.Source = null;

            try
            {
                if (recordAudio.Content.ToString() == "Start Audio Record")
                {
                    audioFile = await Windows.Storage.KnownFolders.VideosLibrary.CreateFileAsync(AUDIO_FILE_NAME, Windows.Storage.CreationCollisionOption.GenerateUniqueName);

                    status.Text = "Audio storage file preparation successful";

                    MediaEncodingProfile recordProfile = null;
                    recordProfile = MediaEncodingProfile.CreateM4a(Windows.Media.MediaProperties.AudioEncodingQuality.Auto);

                    await mediaCapture.StartRecordToStorageFileAsync(recordProfile, audioFile);

                    isRecording = true;
                    recordAudio.IsEnabled = true;
                    recordAudio.Content = "Stop Audio Record";
                    status.Text = "Audio recording in progress... press \'Stop Audio Record\' to stop";
                }
                else
                {
                    status.Text = "Stopping audio recording...";

                    await mediaCapture.StopRecordAsync();

                    isRecording = false;
                    recordAudio.IsEnabled = true;
                    recordAudio.Content = "Start Audio Record";

                    var stream = await audioFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    status.Text = "Playback recorded audio: " + audioFile.Path;
                    playbackElement3.AutoPlay = true;
                    playbackElement3.SetSource(stream, audioFile.FileType);
                    playbackElement3.Play();
                }
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                Cleanup();
            }
            finally
            {
                recordAudio.IsEnabled = true;
            }
        }

        /// <summary>
        /// Callback function for any failures in MediaCapture operations
        /// </summary>
        /// <param name="currentCaptureObject"></param>
        /// <param name="currentFailure"></param>
        private async void mediaCapture_Failed(MediaCapture currentCaptureObject, MediaCaptureFailedEventArgs currentFailure)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    status.Text = "MediaCaptureFailed: " + currentFailure.Message;

                    if (isRecording)
                    {
                        await mediaCapture.StopRecordAsync();
                        status.Text += "\n Recording Stopped";
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    SetInitButtonVisibility(Action.DISABLE);
                    SetVideoButtonVisibility(Action.DISABLE);
                    SetAudioButtonVisibility(Action.DISABLE);
                    status.Text += "\nCheck if camera is diconnected. Try re-launching the app";
                }
            });
        }

        /// <summary>
        /// Callback function if Recording Limit Exceeded
        /// </summary>
        /// <param name="currentCaptureObject"></param>
        public async void mediaCapture_RecordLimitExceeded(Windows.Media.Capture.MediaCapture currentCaptureObject)
        {
            try
            {
                if (isRecording)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        try
                        {
                            status.Text = "Stopping Record on exceeding max record duration";
                            await mediaCapture.StopRecordAsync();
                            isRecording = false;
                            recordAudio.Content = "Start Audio Record";
                            recordVideo.Content = "Start Video Record";
                            if (mediaCapture.MediaCaptureSettings.StreamingCaptureMode == StreamingCaptureMode.Audio)
                            {
                                status.Text = "Stopped record on exceeding max record duration: " + audioFile.Path;
                            }
                            else
                            {
                                status.Text = "Stopped record on exceeding max record duration: " + recordStorageFile.Path;
                            }
                        }
                        catch (Exception e)
                        {
                            status.Text = e.Message;
                        }
                    });
                }
            }
            catch (Exception e)
            {
                status.Text = e.Message;
            }
        }

          
     


    }


}
