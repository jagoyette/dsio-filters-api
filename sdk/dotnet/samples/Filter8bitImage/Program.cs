/// This sample illustrates how to use the DSIO Filter Api with
/// an 8-bit image. We assume that the 8-bit image data does not
/// have a nonlinear map applied.
/// 
/// The 8-bit image is upscaled to 12-bits and a LutInfo object
/// is constructed representing the 12-bit image.
/// 
/// The DSIO Filter is called and the resulting 16-bit image is
/// scaled back to 8-bit and saved as a PNG
/// 
/// This sample uses a NuGet package from ImageSharp for image processing
/// 

using DSIO.Filters.Api.Sdk.Client.V1;
using Filter8bitImage;


// API Keys are long, modify Console to get around 254 char limit
Console.SetIn(new StreamReader(Console.OpenStandardInput(8192)));

Console.WriteLine("Sample console app for Modality Api");

// Read credentials
Console.Write("Enter your API Username: ");
var username = Console.ReadLine();
Console.Write("Enter your API Key: ");
var apikey = Console.ReadLine();

// Create service proxy and set credentials
var service = new ServiceProxy();
service.SetBasicAuthenticationHeader(username, apikey);


// ServiceProxy HttpClient calls will throw exceptions when
// unsuccessful. Handle exceptions and show errors
try
{
    // Test availability
    Console.WriteLine("Checking availability of service...");
    var isAvailable = await service.IsServiceAvailable();
    Console.WriteLine($"Modality Api V1 service isAvailable: {isAvailable}");

    if (isAvailable)
    {
        Console.Write("Enter full path to an 8-bit PNG image: ");
        var inputFilepath = Console.ReadLine();

        // Check for existing file
        if (File.Exists(inputFilepath))
        {
            // Create a filepath to store the converted image
            var parentFolder = Path.GetDirectoryName(inputFilepath)!;
            var fileName = Path.GetFileName(inputFilepath)!;
            var scaled12Filepath = Path.Combine(parentFolder, $"{fileName}.scaled12.png");

            // Convert the 8-bit image to 12-bit range and save as a 16-bit PNG
            Console.WriteLine($"Converting image to 12 bits file {scaled12Filepath}...");
            ImageDepthConverter.Convert8BitGrayTo12BitGray(inputFilepath, scaled12Filepath);

            // Compute the min/max gray levels in 12-bit range
            (var minGray, var maxGray) = ImageDepthConverter.GetMinMaxGrayScale(scaled12Filepath);

            // create a stream from the 12-bit image file
            var fileStream = new FileStream(scaled12Filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var imageContent = new StreamContent(fileStream);

            // Create an ImageInfo describing the 12-bit image. We hardcode Binning to Unbinned in
            // this sample and we set LutInfo to a linear map. Note that we reverse the min/max grays.
            // This is done because the DSIO Filter Service will operate on inverted data. By swapping
            // the min/max gray, we end up with the expected map instead of an inverted map
            var imageInfo = new DSIO.Filters.Api.Sdk.Types.V1.ImageInfo
            {
                AcquisitionInfo = new DSIO.Filters.Api.Sdk.Types.V1.AcquisitionInfo
                {
                    Binning = DSIO.Filters.Api.Sdk.Types.V1.BinningMode.Unbinned
                },
                LutInfo = new DSIO.Filters.Api.Sdk.Types.V1.LutInfo
                {
                    Gamma = 1.0,
                    Slope = 4095,
                    Offset = 0,
                    TotalGrays = 4096,
                    MinimumGray = maxGray,
                    MaximumGray = minGray
                }
            };

            // Create an image resource
            var imageResource = await service.UploadImage(imageInfo, imageContent, "image/png");

            // Apply the filter with desired parameters
            var parameters = new DSIO.Filters.Api.Sdk.Types.V1.SupremeFilterParameters
            {
                Task = DSIO.Filters.Api.Sdk.Types.V1.SupremeFilterParameters.TaskNames.General,
                Sharpness = 70
            };

            Console.WriteLine($"Applying filter...");
            using (var filteredStream = await service.SupremeFilter(imageResource.Id, parameters))
            {
                // Convert the result from 16-bit back down to 8-bit and save result
                var outputFilepath = Path.Combine(parentFolder, $"{fileName}.filtered.png");

                Console.WriteLine($"Saving result file {outputFilepath}...");
                ImageDepthConverter.Convert16BitGrayTo8BitGray(filteredStream, outputFilepath);
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Exception encountered: {ex.Message}");
}

Console.WriteLine("Press Enter to exit.");
Console.ReadLine();
