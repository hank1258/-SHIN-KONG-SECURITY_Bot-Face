using AForge.Video;
using AForge.Video.DirectShow;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WebcamUserControl;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using WebCamUserControl;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing.Drawing2D;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace WebcamUserControl
{
    public partial class VideoPortControl : UserControl
    {
        
        Microsoft.ProjectOxford.Face.FaceServiceClient faceClient = new FaceServiceClient("8f7a031e5133417aa8b1f1ab525efec1");
        // Create grabber. 
       // FrameGrabber<Face[]> grabber = new FrameGrabber<Face[]>();
        private readonly FrameGrabber<LiveCameraResult> _grabber = null;
        private LiveCameraResult _latestResultsToDisplay = null;
        public  string GroupName = "mtcbotdemo";
        public string ShinKuanTestPersonID = "f7d3f0af-7866-4f2f-80eb-d8d815e8e735";
        public string ShinKuanTestPersistedFaceId = "8477e7fa-529e-4c43-9e6e-54e6264a36d1";
        public string Face_Directory = "Face";
        public string Card_Directory = "Card";
        public string Face_File = "faceimg.png";
        public string Card_File = "man_mature.png";
        private static string BLOB_CONTAINER_STRING = "shinkongcontainer";
        private static string BLOB_CONNECTION_STRING = "DefaultEndpointsProtocol=https;AccountName=shinkong;AccountKey=pyl//qs7YQ2VPm1Dl7/8hw5ObceaMTamfobzTvOajmCQyWzWxuS1NYThvfp4HLYkeNRjJYeQ5rc7zZ38YR/Szw==;";
        private ObservableCollection<Person> Persons = new ObservableCollection<Person>();
        private bool _fuseClientRemoteResults;
        private static readonly ImageEncodingParam[] s_jpegParams = {
            new ImageEncodingParam(OpenCvSharp.ImageEncodingID.JpegQuality,60) //ImwriteFlags.JpegQuality, 60)
        };
        public VideoPortControl()
        {
            InitializeComponent();
            // Create grabber. 
            _grabber = new FrameGrabber<LiveCameraResult>();
            // Set up a listener for when the client receives a new frame.
            _grabber.NewFrameProvided += (s, e) =>
            {
               // Console.WriteLine("ddvvvvvvvvvvvvvvvvvvvvvvvvv");
                // The callback may occur on a different thread, so we must use the
                // MainWindow.Dispatcher when manipulating the UI. 
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    var device = (FilterInfo)VideoDevicesComboBox.SelectedItem;
                    // Display the image in the left pane.
                     LeftImage.Source = e.Frame.Image.ToBitmapSource();
                 //   Console.WriteLine("ddddddddddddddddd");
                    //videoSourcePlayer.VideoSource= e.Frame.Image.ToBitmapSource();

                    // If we're fusing client-side face detection with remote analysis, show the
                    // new frame now with the most recent analysis available. 
                    /* if (_fuseClientRemoteResults)
                     {
                         RightImage.Source = VisualizeResult(e.Frame);
                     }*/
                }));

                // See if auto-stop should be triggered. 
             /*   if (Properties.Settings.Default.AutoStopEnabled && (DateTime.Now - _startTime) > Properties.Settings.Default.AutoStopTime)
                {
                    _grabber.StopProcessingAsync();
                }*/
            };

            // Set up a listener for when the client receives a new result from an API call. 
            _grabber.NewResultAvailable += (s, e) =>
            {
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (e.TimedOut)
                    {
                        // MessageArea.Text = "API call timed out.";
                        Console.WriteLine("api time out");
                    }
                    else if (e.Exception != null)
                    {
                        string apiName = "";
                        string message = e.Exception.Message;
                        var faceEx = e.Exception as FaceAPIException;
                        var emotionEx = e.Exception as Microsoft.ProjectOxford.Common.ClientException;
                        var visionEx = e.Exception as Microsoft.ProjectOxford.Vision.ClientException;
                        if (faceEx != null)
                        {
                            apiName = "Face";
                            message = faceEx.ErrorMessage;
                        }
                        else if (emotionEx != null)
                        {
                            apiName = "Emotion";
                            message = emotionEx.Error.Message;
                        }
                        else if (visionEx != null)
                        {
                            apiName = "Computer Vision";
                            message = visionEx.Error.Message;
                        }
                       // MessageArea.Text = string.Format("{0} API call failed on frame {1}. Exception: {2}", apiName, e.Frame.Metadata.Index, message);
                    }
                    else
                    {
                        _latestResultsToDisplay = e.Analysis;

                        // Display the image and visualization in the right pane. 
                       /* if (!_fuseClientRemoteResults)
                        {
                            RightImage.Source = VisualizeResult(e.Frame);
                        }*/
                    }
                }));
            };
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            foreach (var d in videoDevices)
                VideoDevicesComboBox.Items.Add(d);

            VideoDevicesComboBox.SelectedIndex = 0;

        }

        public string OverlayImagePath { get; set; }






        /// <summary>
        /// Displays webcam video and asks for image to overlay
        /// </summary>
        public async void StartVideoFeed()
        {
            var openFileDialog = new OpenFileDialog()
            {
                DefaultExt = "png",
                Filter = "PNG Image | *.png",
                Title = "Please select an image to overlay onto the video feed..."
            };

            if (openFileDialog.ShowDialog() == true)
                OverlayImagePath = openFileDialog.FileName;

            var device = (FilterInfo)VideoDevicesComboBox.SelectedItem;
            if (device != null)
            {
                var source = new VideoCaptureDevice(device.MonikerString);
                // register NewFrame event handler
                Console.WriteLine("ppppp");
            //    source.NewFrame += new NewFrameEventHandler(video_NewFrame);

             //   videoSourcePlayer.VideoSource = source;
              //  videoSourcePlayer.Start();
            }


            // How often to analyze. 
            _grabber.TriggerAnalysisOnInterval(Properties.Settings.Default.AnalysisInterval);
            _grabber.AnalysisFunction = FacesAnalysisFunction;
            await _grabber.StartProcessingCameraAsync(VideoDevicesComboBox.SelectedIndex);
        }

        /// <summary>
        /// Stops the display of webcam video.
        /// </summary>
        public void StopVideoFeed()
        {
            videoSourcePlayer.SignalToStop();
        }

        /// <summary>
        /// Saves a snapshot of current webcam video frame.
        /// </summary>
        public void SaveSnapshot()
        {
            using (Bitmap bmp = videoSourcePlayer.GetCurrentVideoFrame())
            {
                var saveFileDialog = new SaveFileDialog()
                {
                    Filter = "PNG Image | *.png",
                    DefaultExt = "png"
                };

                if (saveFileDialog.ShowDialog() == true)
                    bmp.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        public Stream GetStream(System.Drawing.Image img, ImageFormat format)
        {
            var ms = new MemoryStream();
            img.Save(ms, format);
            return ms;
        }

        private async Task<LiveCameraResult> FacesAnalysisFunction(VideoFrame frame)
        {
            

            // Encode image. 
            var framestream = frame.Image.ToMemoryStream(".jpg", s_jpegParams);

            Bitmap temp_frame = new Bitmap(frame.Image.ToMemoryStream(".jpg", s_jpegParams));
           
            // im.Save("image.bmp");

            // Submit image to API. 
            var attrs = new List<FaceAttributeType> { FaceAttributeType.Age,
                FaceAttributeType.Gender, FaceAttributeType.HeadPose };
            var faces = await faceClient.DetectAsync(framestream, true,true,returnFaceAttributes: attrs);
            // Count the API call. 
            int i;
            for( i =0 ; i < faces.Length ; i++){
                Console.WriteLine("age : "+faces[i].FaceAttributes.Age);
                Console.WriteLine("gender : "+faces[i].FaceAttributes.Gender);
            }


            var identifyResult = await faceClient.IdentifyAsync(GroupName, faces.Select(ff => ff.FaceId).ToArray());
            for (int idx = 0; idx < faces.Length; idx++)
            {
                // Update identification result for rendering
             
                var res = identifyResult[idx];
                if (res.Candidates.Length > 0) //&& Persons.Any(p => p.PersonId == res.Candidates[0].PersonId.ToString()))
                {
                    //  face.PersonName = Persons.Where(p => p.PersonId == res.Candidates[0].PersonId.ToString()).First().PersonName;
                    Console.WriteLine("hi hank");
                    WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();
       
                    wplayer.URL = "3022.mp3";
                    wplayer.controls.play();

                    Bitmap CroppedImage = null;
                    if (faces[idx].FaceAttributes.HeadPose.Roll >= 10 || faces[idx].FaceAttributes.HeadPose.Roll <= -10)
                    {
                        System.Drawing.Rectangle rect = new System.Drawing.Rectangle(Convert.ToInt32(faces[idx].FaceRectangle.Left), Convert.ToInt32(faces[idx].FaceRectangle.Top), faces[idx].FaceRectangle.Width, faces[idx].FaceRectangle.Height);

                        CroppedImage = new Bitmap(CropRotatedRect(temp_frame, rect, Convert.ToSingle(faces[idx].FaceAttributes.HeadPose.Roll * -1), true));
                    }
                    else
                    {
                        CroppedImage = new Bitmap(temp_frame.Clone(new System.Drawing.Rectangle(faces[idx].FaceRectangle.Left, faces[idx].FaceRectangle.Top, faces[idx].FaceRectangle.Width, faces[idx].FaceRectangle.Height), temp_frame.PixelFormat));

                    }


                    //Save to local
                    String FacePath = Directory.GetCurrentDirectory() + "\\" + Face_Directory;
                    if (!Directory.Exists(FacePath))
                    {
                        Directory.CreateDirectory(FacePath);
                    }
                    StringBuilder st = new StringBuilder();
                    st.Append(FacePath+"\\"+Face_File);   
                    string outputFileName = st.ToString();
                    using (MemoryStream memory = new MemoryStream())
                    {
                        using (FileStream fs = new FileStream(outputFileName, FileMode.Create, FileAccess.ReadWrite))
                        {
                            CroppedImage.Save(memory, ImageFormat.Png);
                            byte[] bytes = memory.ToArray();
                            fs.Write(bytes, 0, bytes.Length);
                            fs.Flush();
                            fs.Close();
                            memory.Flush();
                            memory.Close();
                        }

                    }

                    //Upload To Blob
                    string filename = Face_Directory + "\\" + Face_File;
     
                    string path;
                    if (Path.GetPathRoot(filename) != null && Path.GetPathRoot(filename) != "")
                        path = filename.Replace(Path.GetPathRoot(filename), "").Replace("\\", "/");
                    else
                        path = filename.Replace("\\", "/");

                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(BLOB_CONNECTION_STRING);
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = blobClient.GetContainerReference(BLOB_CONTAINER_STRING);
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(path);
                    using (var fileStream = System.IO.File.OpenRead(filename))
                    {
                        blockBlob.UploadFromStream(fileStream);
                    }



                    String CardPath = Directory.GetCurrentDirectory() + "\\" + Card_Directory;
                    if (!Directory.Exists(CardPath))
                    {
                        Directory.CreateDirectory(CardPath);
                    }
                    StringBuilder card_st = new StringBuilder();
                    card_st.Append(CardPath + "\\" + Card_File);
                    string body_img_path = card_st.ToString();
                    System.Drawing.Image body_face;

                    using (System.Drawing.Image body_frame = System.Drawing.Image.FromFile(body_img_path.ToString()))
                    {
                        using (var bitmap = new Bitmap(body_frame.Width, body_frame.Height))
                        {
                            using (var canvas = Graphics.FromImage(bitmap))
                            {
                                canvas.DrawImage(body_frame,
                                                new System.Drawing.Rectangle(0,
                                                              0,
                                                              body_frame.Width,
                                                              body_frame.Height),
                                                new System.Drawing.Rectangle(0,
                                                              0,
                                                              body_frame.Width,
                                                              body_frame.Height),
                                                GraphicsUnit.Pixel);

                                body_face = System.Drawing.Image.FromFile(FacePath + "\\" + Face_File);
                                int faceX = 740;
                                int faceY = 530;
                                int faceWidth = 333;
                                int faceHeight = 360;

                                if (faces[idx].FaceAttributes.HeadPose.Yaw > 0)//looking right
                                {
                                    body_face.RotateFlip(RotateFlipType.Rotate180FlipY);
                                }

                                canvas.DrawImage(body_face, faceX, faceY, faceWidth, faceHeight);
                                string hat_img_path = body_img_path.Replace(".png", "_hat.png");
                                System.Drawing.Image hatImg = System.Drawing.Image.FromFile(hat_img_path.ToString());
                                canvas.DrawImage(hatImg,
                                                new System.Drawing.Rectangle(0,
                                                                0,
                                                                body_frame.Width,
                                                                body_frame.Height),
                                                new System.Drawing.Rectangle(0,
                                                                0,
                                                                body_frame.Width,
                                                                body_frame.Height),
                                                GraphicsUnit.Pixel);
                                canvas.Save();
                            }
                            if (faces[idx].FaceAttributes.HeadPose.Yaw > 0)//looking right
                            {
                                bitmap.RotateFlip(RotateFlipType.Rotate180FlipY);
                            }

                            try
                            {


                                string fi_path = Face_Directory + "\\" + "final.png";
                              
                                using (Bitmap tempBitmap = new Bitmap(bitmap))
                                {
                                    using (Bitmap resizedBitmap = new Bitmap(tempBitmap, new System.Drawing.Size((int)(1858 * 0.3f), (int)(2480 * 0.3f))))
                                    {
                                        resizedBitmap.Save(fi_path, System.Drawing.Imaging.ImageFormat.Png);
                                       
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                System.Console.WriteLine(ex);
                            }


                        }
                    }
              
                    StringBuilder slide_st = new StringBuilder();
                    slide_st.Append(CardPath + "\\" + "Slide.png");
                    string slide_img_path = slide_st.ToString();
                    using (System.Drawing.Image slide_frame = System.Drawing.Image.FromFile(slide_img_path.ToString()))
                    {
                        using (var bitmap = new Bitmap(slide_frame.Width, slide_frame.Height))
                        {
                            using (var canvas = Graphics.FromImage(bitmap))
                            {

                                canvas.DrawImage(slide_frame,
                                             new System.Drawing.Rectangle(0,
                                                           0,
                                                          slide_frame.Width,
                                                          slide_frame.Height),
                                             new System.Drawing.Rectangle(0,
                                                           0,
                                                          slide_frame.Width,
                                                          slide_frame.Height),
                                             GraphicsUnit.Pixel);
                                String fi_path = Face_Directory + "\\" + "final.png";
                                System.Drawing.Image temp_body = System.Drawing.Image.FromFile(fi_path);

                                int dx = 520;
                                int dy = 100;
                                //canvas.DrawImage(temp_body, dx, dy, Constants.FIGURE_WIDTH * Constants.resizeRatio, Constants.FIGURE_HEIGHT * Constants.resizeRatio);
                                canvas.DrawImage(temp_body, dx, dy, 1858 * 0.3f, 2480 * 0.3f);


                                canvas.Save();


                                System.Console.WriteLine("finish fig bitmap");

                            }
                            try
                            {


                                string fi_path = Face_Directory + "\\" + "Final_Card.png";

                                using (Bitmap tempBitmap = new Bitmap(bitmap))
                                {
                                    using (Bitmap resizedBitmap = new Bitmap(tempBitmap, new System.Drawing.Size((int)(bitmap.Width), (int)(bitmap.Height))))
                                    {
                                        resizedBitmap.Save(fi_path, System.Drawing.Imaging.ImageFormat.Png);

                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                System.Console.WriteLine(ex);
                            }

                        }
                    }
                    
                }
                else
                {
                  //  face.PersonName = "Unknown";
                }
            }

            // Output. 
            return new LiveCameraResult { Faces = faces };
        }

        public static Bitmap CropRotatedRect(Bitmap source, System.Drawing.Rectangle rect, float angle, bool HighQuality)
        {
            Bitmap result = new Bitmap(rect.Width, rect.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = HighQuality ? InterpolationMode.HighQualityBicubic : InterpolationMode.Default;
                using (System.Drawing.Drawing2D.Matrix mat = new System.Drawing.Drawing2D.Matrix())
                {
                    mat.Translate(-rect.Location.X, -rect.Location.Y);
                    System.Drawing.Point p = new System.Drawing.Point(rect.Location.X + rect.Width / 2, rect.Location.Y + rect.Height / 2);
                    mat.RotateAt(angle, p);
                    g.Transform = mat;
                    g.DrawImage(source, new System.Drawing.Point(0, 0));
                }
            }
            return result;
        }


        private async void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(OverlayImagePath))
            {
                var frame = eventArgs.Frame; // reference to the current frame   
               
                var g = Graphics.FromImage(frame);
                //  await faceClient.DetectAsync(frame.Image.ToMemoryStream(".jpg"));
                using (System.Drawing.Image backImage = (Bitmap)frame.Clone())
                using (System.Drawing.Image frontImage = System.Drawing.Image.FromFile(OverlayImagePath))
                using (System.Drawing.Image newImage = new Bitmap(backImage.Width, backImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    Stream faceimagestream = GetStream(newImage, System.Drawing.Imaging.ImageFormat.Jpeg);
                    try
                    {

                       // Console.WriteLine(faceimagestream);
                     //   Microsoft.ProjectOxford.Face.Contract.Face[] faces = await faceserviceclient.DetectAsync(faceimagestream);//, true, true,

                        //new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.HeadPose, FaceAttributeType.Glasses });
                      //  Console.WriteLine("asdasdasdasdasdasdasd");
                        //if (faces.Length >= 0)

                        //{

                        //  System.Console.WriteLine("There is no face in current frame");

                        // return;

                        //}
                    }
                    catch (Exception ex)
                    {

                        System.Console.WriteLine(ex);

                    }


                    using (Graphics compositeGraphics = Graphics.FromImage(newImage))
                    {
                        compositeGraphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                        compositeGraphics.DrawImageUnscaled(backImage, 0, 0);
                        compositeGraphics.DrawImageUnscaled(frontImage, 250, 350); // TODO: make positioning dynamic or configurable
                        compositeGraphics.Dispose();
                        frontImage.Dispose();
                        backImage.Dispose();
                    }

                    g.DrawImage(newImage, new PointF(0, 0));
                    g.Dispose();
                }
            }
        }
    }
}