using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Demo.AwsS3Minio.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Demo.AwsS3Minio.Web.Controllers
{
    public class HomeController : Controller
    {
        private const string keyName = "3.mp4";
        private const string filePath = null;
        // Specify your bucket region (an example region is shown).  
        private static readonly string bucketName = ConfigurationManager.AppSettings["BucketName"];
        //private static readonly RegionEndpoint bucketRegion = RegionEndpoint.APSoutheast1;
        private static readonly string accesskey = ConfigurationManager.AppSettings["AWSAccessKey"];
        private static readonly string secretkey = ConfigurationManager.AppSettings["AWSSecretKey"];

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult AddBucket()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AddBucket(CreateBucketModel model)
        {
            var client = GetStorageSettings();

            if (ModelState.IsValid)
            {
                PutBucketRequest request = new PutBucketRequest();
                request.BucketName = model.Name;
                client.PutBucket(request);
            }
            return RedirectToAction("AddBucket");
        }

        [HttpGet]
        public async Task<ActionResult> DeleteBucket()
        {
            var bucketList = new List<string>();
            var client = GetStorageSettings();
            var listBucketResponse = await client.ListBucketsAsync();

            foreach (var bucket in listBucketResponse.Buckets)
            {
                bucketList.Add(bucket.BucketName);
            }
            ViewBag.BucketList = bucketList;
            return View();
        }

        [HttpPost]
        public ActionResult DeleteBucket(DeleteBucketModel model)
        {
            var client = GetStorageSettings();
            if (ModelState.IsValid)
            {
                DeleteBucketRequest request = new DeleteBucketRequest();
                request.BucketName = model.Name;
                client.DeleteBucket(request);
            }
            return RedirectToAction("DeleteBucket");
        }


        [HttpGet]
        public async Task<ActionResult> GetFiles()
        {
            Dictionary<string, List<S3Object>> dictionary = new Dictionary<string, List<S3Object>>();

            var client = GetStorageSettings();

            var listBucketResponse = await client.ListBucketsAsync();

            foreach(var bucket in listBucketResponse.Buckets)
            {
                var objectList = new List<S3Object>();
                var listObjectsResponse = await client.ListObjectsAsync(bucket.BucketName);
                foreach(var obj in listObjectsResponse.S3Objects)
                {
                    objectList.Add(obj);
                }

                dictionary.Add(bucket.BucketName, objectList);
            }

            ViewBag.BucketObjLists = dictionary;

            return View();
        }

        [HttpGet]
        public ActionResult UploadFile()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase file)
        {
            var client = GetStorageSettings();

            var fileTransferUtility = new TransferUtility(client);
            try
            {
                if (file.ContentLength > 0)
                {
                    var filePath = Path.Combine(Server.MapPath("~/Files"), Path.GetFileName(file.FileName));
                    file.SaveAs(filePath);

                    var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = bucketName,
                        FilePath = filePath,
                        //StorageClass = S3StorageClass.StandardInfrequentAccess,
                        PartSize = 6291456, // 6 MB.
                        //Key = keyName,
                        CannedACL = S3CannedACL.PublicRead
                    };
                    //fileTransferUtilityRequest.Metadata.Add("param1", "Value1");
                    //fileTransferUtilityRequest.Metadata.Add("param2", "Value2");
                    fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
                    fileTransferUtility.Dispose();
                }
                ViewBag.Message = "File Uploaded Successfully!!";
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                    ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    ViewBag.Message = "Check the provided AWS Credentials.";
                }
                else
                {
                    ViewBag.Message = "Error occurred: " + amazonS3Exception.Message;
                }
            }
            return RedirectToAction("UploadFile");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public AmazonS3Client GetStorageSettings()
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.APSoutheast1, // MUST set this before setting ServiceURL and it should match the `MINIO_REGION` environment variable.
                ServiceURL = "http://192.168.3.3:9000", // replace http://localhost:9000 with URL of your MinIO server AWS: https://s3.ap-southeast-1.amazonaws.com
                ForcePathStyle = true // MUST be true to work correctly with MinIO server
            };

            var s3Client = new AmazonS3Client(accesskey, secretkey, config);

            return s3Client;
        }
    }
}