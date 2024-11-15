using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System;
using System.IO;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using OpenCvSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace HelloWorld;
 public class Function
{

    private static readonly HttpClient client = new HttpClient();

    private static async Task<string> GetCallingIP()
    {
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");

        var msg = await client.GetStringAsync("http://checkip.amazonaws.com/").ConfigureAwait(continueOnCapturedContext:false);

        return msg.Replace("\n","");
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {

            var location = await GetCallingIP();
            var body = new Dictionary<string, string>
            {
                { "message", "hello world" },
                { "location", location }
            };
            // Get the current directory (where the executable is located)
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            // Get all files in the current directory that have a ".jpg" extension
            string[] jpgFiles = Directory.GetFiles("/home/user/foul-catholic/foul-catholic/src/HelloWorld", "*.jpg");

            List<byte[]> jpgByteArrays = new List<byte[]>();

            // Check if any files were found
            if (jpgFiles.Length > 0)
            {
                
                Console.WriteLine("Found .jpg files:");
                foreach (string file in jpgFiles)
                {
                    var fileBytes = File.ReadAllBytes(file);
                    // Read the image
                    Mat image = Cv2.ImRead(file);

                    // Ensure the image is loaded
                    if (image.Empty())
                    {
                        Console.WriteLine("Could not open or find the image.");
                    }

                // Convert the image to grayscale
                    Mat grayImage = new Mat();
                    Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);

                    // Apply a stronger Gaussian Blur to smooth edges more effectively (larger kernel)
                    Mat blurredImage = new Mat();
                    Cv2.GaussianBlur(grayImage, blurredImage, new OpenCvSharp.Size(15, 15), 0);  // Larger kernel for more smoothing

                    // Perform Canny edge detection (tuned thresholds for soft edges)
                    Mat edges = new Mat();
                    Cv2.Canny(blurredImage, edges, 50, 150);  // Adjusted thresholds for better edge capture

                    // Optional: Dilate edges to connect broken edges (useful for rounded corners)
                    Mat dilatedEdges = new Mat();
                    Cv2.Dilate(edges, dilatedEdges, new Mat(), iterations: 88);  // Increased iterations for better connection

                    // Optional: Erode edges to clean up noise (helps with rounder contours)
                    Mat erodedEdges = new Mat();
                    Cv2.Erode(dilatedEdges, erodedEdges, new Mat(), iterations: 1);  // Erosion to reduce small spurious edges

                    // Find contours based on edges
                    OpenCvSharp.Point[][] contours;
                    HierarchyIndex[] hierarchy;
                    Cv2.FindContours(erodedEdges, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    //WORKS
                    // Find the bounding box of the largest contour
                    Rect boundingBox = new Rect();

                    foreach (var contour in contours)
                    {
                        if (contour.Length >= 5) // Ignore small contours
                        {
                            var contourRect = Cv2.BoundingRect(contour);

                            // if (contourRect.Width * contourRect.Height > boundingBox.Width * boundingBox.Height)
                            // {
                            // Increase the bounding box by a fixed margin
                            int margin = 10;
                            Rect expandedBoundingBox = new Rect(
                                contourRect.X - margin, 
                                contourRect.Y - margin, 
                                contourRect.Width + 2 * margin, 
                                contourRect.Height + 2 * margin
                            );

                            // Ensure the bounding box is within the image dimensions
                            expandedBoundingBox.X = Math.Max(expandedBoundingBox.X, 0);
                            expandedBoundingBox.Y = Math.Max(expandedBoundingBox.Y, 0);
                            expandedBoundingBox.Width = Math.Min(expandedBoundingBox.Width, image.Width - expandedBoundingBox.X);
                            expandedBoundingBox.Height = Math.Min(expandedBoundingBox.Height, image.Height - expandedBoundingBox.Y);

                            boundingBox = expandedBoundingBox;
                                
                                // Crop the image using the bounding box
                                if (boundingBox.Width > 0 && boundingBox.Height > 0)
                                {
                                    Mat croppedImage = new Mat(image, boundingBox);

                                    Guid randomId = Guid.NewGuid();
                                    // Save the cropped image
                                    Cv2.ImWrite("/home/user/foul-catholic/foul-catholic/src/HelloWorld/hello/"+file.Split('/').Last()+"---"+randomId+".jpg", croppedImage);
                                    Console.WriteLine($"Cropped image saved to {"/home/user/foul-catholic/foul-catholic/src/HelloWorld/hello"}");

                                    FileInfo croppedFileInfo = new FileInfo("/home/user/foul-catholic/foul-catholic/src/HelloWorld/hello/"+file.Split('/').Last()+"---"+randomId+".jpg");
                                    

                                }
                                else
                                {
                                    Console.WriteLine("No contours found.");
                                }
                            // }
                        }
                    }

                    // Clean up
                    image.Dispose();
                    grayImage.Dispose();
                    blurredImage.Dispose();
                    edges.Dispose();
                // // Read the file as a byte array
                // byte[] fileBytes = File.ReadAllBytes(file);

                // Mat imageMat = CvInvoke.Imread(file, ImreadModes.Color);
                // Mat grayImage = new Mat();
                // CvInvoke.CvtColor(imageMat, grayImage, ColorConversion.Bgr2Gray);

                // Mat blurredImage = new Mat();
                // CvInvoke.GaussianBlur(grayImage, blurredImage, new System.Drawing.Size(5, 5), 1.5);

                // Mat edges = new Mat();

                // CvInvoke.Canny(blurredImage, edges, 100, 200);

                // VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                // Mat hierarchy = new Mat();
                // CvInvoke.FindContours(edges, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                // // Mat Image = new Mat(1, fileBytes.Length, DepthType.Cv8U, 1);
                //         // Assuming the largest contour is the bounding box for the card
                // RotatedRect boundingRect = FindBoundingBox(contours);

                // // if (boundingRect is not null)
                // // {
                //     // Load the image into SixLabors.ImageSharp
                // using (Image<Rgba32> image = Image.Load<Rgba32>(fileBytes))
                // {
                //     // Create a rectangle for cropping based on bounding box
                //     Rectangle cropRectangle = new Rectangle(
                //         (int)boundingRect.MinAreaRect().X - (int)boundingRect.Size.Width / 2,
                //         (int)boundingRect.MinAreaRect().Y - (int)boundingRect.Size.Height / 2,
                //         (int)boundingRect.Size.Width,
                //         (int)boundingRect.Size.Height
                //     );

                //     // Crop the image using ImageSharp
                //     image.Mutate(ctx => ctx.Crop(cropRectangle));

                //     // Save the cropped image
                //     image.Save("cropped_image.jpg", new JpegEncoder());

                //     Console.WriteLine("Image cropped successfully!");
                // }
                // }
                // else
                // {tempCroppedFilePath

                // // Check if the image is loaded correctly
                // if (Image.IsEmpty)
                // {
                //     Console.WriteLine("Could not load image from byte array.");
                //     return new APIGatewayProxyResponse
                //     {
                //         Body = JsonSerializer.Serialize(body),
                //         StatusCode = 200,
                //         Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                //     };
                // }

                // Mat grayImage = new Mat();
                // CvInvoke.CvtColor(Image, grayImage, ColorConversion.Bgr2Gray);

                // // Apply Gaussian blur to reduce noise (helps in edge detection)
                // Mat blurredImage = new Mat();
                // CvInvoke.GaussianBlur(grayImage, blurredImage, new System.Drawing.Size(5, 5), 1.5);

                // // Perform Canny edge detection
                // Mat edges = new Mat();
                // double threshold1 = 50; // Lower threshold for edge detection
                // double threshold2 = 150; // Upper threshold for edge detection
                // CvInvoke.Canny(blurredImage, edges, threshold1, threshold2);


                // Add the byte array to the list
                jpgByteArrays.Add(fileBytes);
            }

        }
        else
        {
            Console.WriteLine("No .jpg files found.");
        }

        return new APIGatewayProxyResponse
        {
            Body = JsonSerializer.Serialize(body),
            StatusCode = 200,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };


        
    }
    
    // // Method to find the largest bounding box from contours
    // static RotatedRect FindBoundingBox(VectorOfVectorOfPoint contours)
    // {
    //     double maxArea = 0;
    //     RotatedRect bestRect = new RotatedRect();

    //     for (int i = 0; i < contours.Size; i++)
    //     {
    //         // Get the current contour
    //         var contour = contours[i];

    //         // Get the bounding box for the contour
    //         RotatedRect rect = CvInvoke.MinAreaRect(contour);

    //         // Check if this is the largest contour based on area
    //         double area = rect.Size.Width * rect.Size.Height;
    //         if (area > maxArea)
    //         {
    //             maxArea = area;
    //             bestRect = rect;
    //         }
    //     }

    //     // Return the best bounding box found
    //     return bestRect;
    // }
}