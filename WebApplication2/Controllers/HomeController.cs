using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Data.SqlClient;
using System.Configuration;
using WebApplication2.Models;
using System.Threading.Tasks;

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {
        string strConnString = ConfigurationManager.ConnectionStrings["connect"].ConnectionString;
        string strLocalString = ConfigurationManager.ConnectionStrings["local"].ConnectionString;

        public ActionResult Index()
        {

            return View();
        }


        /* Press "Connect" -> Create connection */
        [HttpPost]
        public ActionResult Connect_FTP()
        {
            string result;

            ftp ftpClient = new ftp("ftp://ftp.micron.com", "beapps", "Appsbackend1");

            result = ftpClient.checkconnect();
            if (result == "OK")
            {
                ViewBag.msg = "Success connect";
                ViewBag.description = "Success connect to ftp.micron.com";
                /* Release Resources */
                ftpClient = null;

            }
            else
            {
                ViewBag.msg = "Fail connect";
                ViewBag.description = result;
            }

            return View("Index");
        }




        [HttpPost]
        public ActionResult Index(SubconModel model, FormCollection collection)
        {
            var selectedValue = model.SelectSubconName.ToString();
          


            ViewBag.selected_subcon = LogTrace("log", "user select:" + selectedValue.ToString());

            //submit button could not press again
            TempData["select_yn"] = "YES";
            ViewBag.select_done = TempData.Peek("select_yn");

            //start to download(from GAW)
            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string batch_download_path;
            string eMap_download_path;
            string new_batchfilename;
            Boolean GAW = true;
            string GAW_default_path;
            //string GAW_batch_download_path;
            //string GAW_emap_download_path;
            Boolean batchfile_done, eMap_done;
            Boolean download_eMap_fromfiler;
            batchfile_done = false;
            eMap_done = false;
            download_eMap_fromfiler = false;
            //new_batchfilename = "LXB2A2508010_12082022101340";   //this is the example of download from ftp
            //new_batchfilename = "LXB2C0527520_12282022171304";    //this is the example of download from filer
            //new_batchfilename = "LXB243040610_07072022203812";      // 2023/01/03 test case (from filer)
            new_batchfilename = collection["input_batch"];

            ViewBag.selected_subcon = LogTrace("log", "Batch File Name:" + new_batchfilename);

            DirectoryInfo dir1 = Directory.CreateDirectory(userPath + "/reupload_eMap/before");  // create folder

            // judge go to FTP or GAW
            //if (selectedValue.ToString() == "kinsus" | selectedValue.ToString() == "shinko" | selectedValue.ToString() == "sevcmo")
            //{
            //    GAW = true;
            //}

            
            
            
            
            // set up the Simmtech_Japan, Simmtech_Korea, Simmtech_Penang to Simmtech Japan, Simmtech Korea, Simmtech Penang
            if (selectedValue.Contains('_'))
            {
                //Console.WriteLine(selectedValue);
                selectedValue = selectedValue.Replace('_', ' ');
                //Console.WriteLine(selectedValue);
            }
            // set default path on ftp and GAW
            batch_download_path = selectedValue.ToString() + "/outgoing/TAICHUNG_BE/Error/BatchFiles/" + new_batchfilename + ".xml";
            eMap_download_path = selectedValue.ToString() + "/outgoing/TAICHUNG_BE/Error/eMap/" + new_batchfilename + ".zip";
            GAW_default_path = "\\\\sifsgasscitbe.sing.micron.com\\ga_sscitbe\\SSCITBE\\Apps\\CTS\\SubstrateSuppliers\\";





            ftp ftpClient = new ftp("ftp://ftp.micron.com", "beapps", "Appsbackend1");
            ViewBag.selected_subcon = LogTrace("log", "start to download from ftp");

            // download from ftp to c:\users\xxx\reupload_eMap
            string download_result = "";
            if (GAW != true)
            {
                download_result = ftpClient.download(batch_download_path, @userPath + "\\reupload_eMap\\before\\" + new_batchfilename + ".xml");
                ViewBag.selected_subcon = LogTrace("log", download_result + ":batchfile");
            }
            

            // download from Filer
            if (download_result != "ftp download complete")
            {
                // Not found file from FTP
                // download from filer to c:\users\xxx\reupload_eMap
                string batch_filer_path;
              
                    
                batch_filer_path = "\\\\tbfsbeitapp\\tbbeitapp\\Apps\\SupplierStrip\\PROD\\" + selectedValue.ToString() + "\\BatchFiles";
               
                ViewBag.selected_subcon = LogTrace("log", "start to download from filer");

                string destination_path;
                destination_path = FindFilePath(batch_filer_path, new_batchfilename + ".xml");
                if (destination_path != "")
                {
                    ViewBag.selected_subcon = LogTrace("log", "Find batchfile in filer:" + destination_path);

                    //Copy batchfile from filer first
                    System.IO.File.Copy(destination_path + "\\" + new_batchfilename + ".xml", @userPath + "\\reupload_eMap\\before\\" + new_batchfilename + ".xml", true);   //batchfile copy
                    ViewBag.selected_subcon = LogTrace("log", "filer download complete:batchfile");
                    batchfile_done = true;

                    //If eMap found here, "DO NOT NEED TO UNZIP"!!!!!
                    if (Directory.Exists(destination_path.Replace("BatchFiles", "eMap") + "\\" + new_batchfilename))
                    {
                        DirectoryInfo dir2 = Directory.CreateDirectory(userPath + "/reupload_eMap/before/" + new_batchfilename);  // create folder
                        CopyFilesRecursively(destination_path.Replace("BatchFiles", "eMap") + "\\" + new_batchfilename, @userPath + "\\reupload_eMap\\before\\" + new_batchfilename + "\\");   //eMap copy
                        ViewBag.selected_subcon = LogTrace("log", "filer download complete:eMap");
                        eMap_done = true;
                        download_eMap_fromfiler = true;
                    }
                    else
                    {
                        ViewBag.selected_subcon = LogTrace("log", "Error(filer.download):eMap not found.");
                    }
                }// find XML and emap from GAW
                //else if (destination_path == "" & GAW == true)
                //{
                //    GAW_batch_download_path = GAW_default_path + selectedValue.ToString() + "TAICHUNG_BE\\BatchFiles\\archive";
                //    GAW_emap_download_path = GAW_default_path + selectedValue.ToString() + "TAICHUNG_BE\\eMap";

                //    string GAW_destination_path;
                //    GAW_destination_path = FindFilePath(GAW_batch_download_path, new_batchfilename + ".xml");
                //    if (GAW_destination_path != "")
                //    {
                //        //Copy batchfile from filer first
                //        System.IO.File.Copy(GAW_destination_path + "\\" + new_batchfilename + ".xml", @userPath + "\\reupload_eMap\\before\\" + new_batchfilename + ".xml", true);   //batchfile copy
                //        ViewBag.selected_subcon = LogTrace("log", "filer download complete:batchfile");
                //        batchfile_done = true;
                //        CopyFilesRecursively(GAW_emap_download_path + "\\" + new_batchfilename, @userPath + "\\reupload_eMap\\before\\" + new_batchfilename + "\\");   //eMap copy
                //        //Start to unzip eMap                                                                                                                                                                           //Start to unzip eMap                                                                                                                                                                           //Start to unzip eMap
                //        string zipPath = @userPath + "\\reupload_eMap\\before\\" + new_batchfilename + ".zip";
                //        string unzipPath = @userPath + "\\reupload_eMap\\before";
                //        Console.WriteLine(GAW_destination_path);
                //        ViewBag.selected_subcon = LogTrace("log", "Start to unzip eMap");
                //        ZipFile.ExtractToDirectory(zipPath, unzipPath);
                //        ViewBag.selected_subcon = LogTrace("log", "Unzip eMap complete");
                //        eMap_done = true;
                //    }
                //    else
                //    {
                //        ViewBag.selected_subcon = LogTrace("log", "Error(filer.download):batchfile not found.");
                //    }
                //}
                else
                {
                    //ftp and filer could not find this file
                    ViewBag.selected_subcon = LogTrace("log", "Error(filer.download):batchfile not found.");
                }
            }
            else
            {
                batchfile_done = true;
                //download batchfile from ftp complete, continue to download eMap from ftp
                download_result = ftpClient.download(eMap_download_path, @userPath + "\\reupload_eMap\\before\\" + new_batchfilename + ".zip");
                ViewBag.selected_subcon = LogTrace("log", download_result + ":eMap");

                //Start to unzip eMap
                string zipPath = @userPath + "\\reupload_eMap\\before\\" + new_batchfilename + ".zip";
                string unzipPath = @userPath + "\\reupload_eMap\\before";

                ViewBag.selected_subcon = LogTrace("log", "Start to unzip eMap");
                ZipFile.ExtractToDirectory(zipPath, unzipPath);
                ViewBag.selected_subcon = LogTrace("log", "Unzip eMap complete");
                eMap_done = true;

                

            }

            //batchfile & eMap ready, can start rename file and zip eMap
            if (batchfile_done & eMap_done)
            {
                batchfile_done = false;
                eMap_done = false;
                string reupload_filename;
                ViewBag.selected_subcon = LogTrace("log", "-----------------");
                //---------------------------------------------------------------------------------------------------------------------------
                DirectoryInfo dir3 = Directory.CreateDirectory(userPath + "/reupload_eMap/wait_zip");  // create folder
                // start to rename file
                ViewBag.selected_subcon = LogTrace("log", "Start to get reupload filename");
                reupload_filename = GetNewNumber(new_batchfilename);    // not include .xml & .zip
                ViewBag.selected_subcon = LogTrace("log", "Reupload filename:" + reupload_filename);

                ViewBag.selected_subcon = LogTrace("log", "Start to copy reupload batchfilename");
                // batchfile copy
                System.IO.File.Copy(@userPath + "\\reupload_eMap\\before\\" + new_batchfilename + ".xml", @userPath + "\\reupload_eMap\\" + reupload_filename + ".xml", true);
                // eMap copy
                if (download_eMap_fromfiler)
                {
                    DirectoryInfo dir4 = Directory.CreateDirectory(userPath + "/reupload_eMap/wait_zip/" + reupload_filename);  // create folder
                    CopyFilesRecursively(@userPath + "\\reupload_eMap\\before\\" + new_batchfilename, @userPath + "\\reupload_eMap\\wait_zip\\" + reupload_filename);
                }
                else
                {
                    Directory.Move(@userPath + "\\reupload_eMap\\before\\" + new_batchfilename, @userPath + "\\reupload_eMap\\wait_zip\\" + reupload_filename);
                }
                ViewBag.selected_subcon = LogTrace("log", "Move rename batchfile & eMap complete.");

                //---------------------------------------------------------------------------------------------------------------------------
                // start to zip eMap
                string startPath = @userPath + "\\reupload_eMap\\wait_zip";
                string zipPath = @userPath + "\\reupload_eMap\\" + reupload_filename + ".zip";

                ViewBag.selected_subcon = LogTrace("log", "Start to zip eMap");
                //check zip file exist or not
                if (System.IO.File.Exists(zipPath))
                {
                    // if file exist, delete and recreate file
                    System.IO.File.Delete(zipPath);
                    ViewBag.selected_subcon = LogTrace("log", "Zip File alreday exist. Delete existed zip file complete.");
                    ZipFile.CreateFromDirectory(startPath, zipPath);
                }
                else
                {
                    ZipFile.CreateFromDirectory(startPath, zipPath);
                }

                ViewBag.selected_subcon = LogTrace("log", "Zip eMap complete");

                //---------------------------------------------------------------------------------------------------------------------------
                // start to delete eMap folder
                Directory.Delete(@userPath + "\\reupload_eMap\\wait_zip", true);
                ViewBag.selected_subcon = LogTrace("log", "Delete eMap folder complete");

                //---------------------------------------------------------------------------------------------------------------------------
                // before upload, need to check this batch is not exist in database
                using (SqlConnection conn = new SqlConnection(strConnString))
                {
                    conn.Open();
                    SqlCommand scom = new SqlCommand("", conn);
                    scom.CommandText = $@"select * from [ContainerTracking].[dbo].[SupplierBatchMapping] where BatchFileName='{reupload_filename}'";

                    SqlDataReader sread = scom.ExecuteReader();
                    // if batch not exist, can do upload ftp
                    if (sread.HasRows)
                    {
                        ViewBag.selected_subcon = LogTrace("log", "Database check: Batchnumber exist in database. Cannot upload to ftp.");
                    }
                    else
                    {
                        // start to upload to ftp (eMap first, batchfile second)
                        // set default path on ftp
                        string batch_upload_path;
                        string eMap_upload_path;
                        string upload_result;
                        string GAW_batch_upload_path;
                        string GAW_emap_upload_path;

                        ViewBag.selected_subcon = LogTrace("log", "Database check: Batchnumber not exist in database. Can upload to ftp.");


                        // judge the re-upload path is FTP or GAW
                        if (GAW)
                        {
                            ViewBag.selected_subcon = LogTrace("log", "Start to reupload to GoAnyWhere");
                            GAW_emap_upload_path = GAW_default_path + selectedValue.ToString() + "\\TAICHUNG_BE\\eMap\\";
                            GAW_batch_upload_path = GAW_default_path + selectedValue.ToString() + "\\TAICHUNG_BE\\BatchFiles\\";
                            System.IO.File.Copy(@userPath + "\\reupload_eMap\\" + reupload_filename + ".zip", GAW_emap_upload_path + reupload_filename + ".zip", true);
                            ViewBag.selected_subcon = LogTrace("log", "eMap:" + GAW_emap_upload_path);
                            eMap_done = true;
                            System.IO.File.Delete(@userPath + "\\reupload_eMap\\" + reupload_filename + ".zip");
                            ViewBag.selected_subcon = LogTrace("log", "Delete eMap folder complete");
                            Task.Delay(5).Wait();
                            System.IO.File.Copy(@userPath + "\\reupload_eMap\\" + reupload_filename + ".xml", GAW_batch_upload_path + reupload_filename + ".xml", true);
                            ViewBag.selected_subcon = LogTrace("log", "eMap:" + GAW_batch_upload_path);
                            batchfile_done = true;
                            System.IO.File.Delete(@userPath + "\\reupload_eMap\\" + reupload_filename + ".xml");
                            ViewBag.selected_subcon = LogTrace("log", "Delete batch file complete");
                        }
                        else
                        {
                            batch_upload_path = selectedValue.ToString() + "/incoming/TAICHUNG_BE/BatchFiles/" + reupload_filename + ".xml";
                            eMap_upload_path = selectedValue.ToString() + "/incoming/TAICHUNG_BE/eMap/" + reupload_filename + ".zip";

                            // eMap upload
                            upload_result = ftpClient.upload(eMap_upload_path, @userPath + "\\reupload_eMap\\" + reupload_filename + ".zip");
                            ViewBag.selected_subcon = LogTrace("log", "eMap:" + eMap_upload_path);
                            if (upload_result == "ftp upload complete")
                            {
                                eMap_done = true;
                                ViewBag.selected_subcon = LogTrace("log", upload_result + ":eMap");
                            }
                            else
                            {
                                ViewBag.selected_subcon = LogTrace("log", upload_result);
                            }

                            // batchfile upload
                            upload_result = ftpClient.upload(batch_upload_path, @userPath + "\\reupload_eMap\\" + reupload_filename + ".xml");
                            ViewBag.selected_subcon = LogTrace("log", "batchfile:" + batch_upload_path);
                            if (upload_result == "ftp upload complete")
                            {
                                batchfile_done = true;
                                ViewBag.selected_subcon = LogTrace("log", upload_result + ":batchfile");
                            }
                            else
                            {
                                ViewBag.selected_subcon = LogTrace("log", upload_result);
                            }
                        }
                        

                        

                        //---------------------------------------------------------------------------------------------------------------------------
                        // send sucess message
                        if (batchfile_done & eMap_done)
                        {
                            ViewBag.selected_subcon = LogTrace("log", "All tasks complete. Please wait for a while for service processing.");
                        }
                        conn.Close();
                    }
                }

            }



            return View();
        }


        public object LogTrace(string param, string content)
        {
            TempData[param] = TempData.Peek(param) + "[" + DateTime.Now.ToString("hh:mm:ss tt") + "] " + content + "\r\n";
            object log = TempData.Peek(param);

            return log;
        }

        public string FindFilePath(string dic, string filename)
        {
            foreach (string d1 in Directory.GetDirectories(dic))
            {

                if (System.IO.File.Exists(d1 + "\\" + filename))
                {
                    return d1;
                }

            }
            return "";
        }

        public string GetNewNumber(string filename)
        {
            string tmp;
            string result;
            int batch_length;

            string[] subs = filename.Split('_', '.');

            //need to store batchnumber length
            batch_length = subs[1].Length;

            tmp = (Convert.ToInt64(subs[1]) + 1).ToString();
            tmp = tmp.PadLeft(batch_length, '0');
            result = subs[0] + "_" + tmp;

            return result;
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                System.IO.File.Copy(newPath, newPath.Replace(sourcePath, targetPath),true);
            }
        }
    }
}





class ftp
{
    private string host = null;
    private string user = null;
    private string pass = null;
    private FtpWebRequest ftpRequest = null;
    private FtpWebResponse ftpResponse = null;
    private Stream ftpStream = null;
    private int bufferSize = 2048;

    /* Construct Object */
    public ftp(string hostIP, string userName, string password)
    { host = hostIP; user = userName; pass = password; }

    /* Check connection, by Derrick */
    public string checkconnect()
    {
        try
        {
            /* Connect to FTP */
            FtpWebRequest requestDir = (FtpWebRequest)FtpWebRequest.Create(host);
            requestDir.Method = WebRequestMethods.Ftp.ListDirectory;
            requestDir.Credentials = new NetworkCredential(user, pass);
            /* Get response */
            WebResponse response = requestDir.GetResponse();
            return "OK";
        }
        catch (Exception ex)
        {
            return $"Error: {ex}";
        }
    }


    /* Download File */
    public string download(string remoteFile, string localFile)
    {
        try
        {
            /* Create an FTP Request */
            ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + remoteFile);
            /* Log in to the FTP Server with the User Name and Password Provided */
            ftpRequest.Credentials = new NetworkCredential(user, pass);
            /* When in doubt, use these options */
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;
            /* Specify the Type of FTP Request */
            ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;
            /* Establish Return Communication with the FTP Server */
            ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            /* Get the FTP Server's Response Stream */
            ftpStream = ftpResponse.GetResponseStream();
            /* Open a File Stream to Write the Downloaded File */
            FileStream localFileStream = new FileStream(localFile, FileMode.Create);
            /* Buffer for the Downloaded Data */
            byte[] byteBuffer = new byte[bufferSize];
            int bytesRead = ftpStream.Read(byteBuffer, 0, bufferSize);
            /* Download the File by Writing the Buffered Data Until the Transfer is Complete */
            try
            {
                while (bytesRead > 0)
                {
                    localFileStream.Write(byteBuffer, 0, bytesRead);
                    bytesRead = ftpStream.Read(byteBuffer, 0, bufferSize);
                }
            }
            catch (Exception ex) { return $"Error(ftp.download): {ex}"; }
            /* Resource Cleanup */
            localFileStream.Close();
            ftpStream.Close();
            ftpResponse.Close();
            ftpRequest = null;
            return "ftp download complete";
        }
        catch (Exception ex) { return $"Error(ftp.download): {ex}"; }

    }

    /* Upload File */
    public string upload(string remoteFile, string localFile)
    {
        try
        {
            /* Create an FTP Request */
            ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + remoteFile);
            /* Log in to the FTP Server with the User Name and Password Provided */
            ftpRequest.Credentials = new NetworkCredential(user, pass);
            /* When in doubt, use these options */
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;
            /* Specify the Type of FTP Request */
            ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
            /* Establish Return Communication with the FTP Server */
            ftpStream = ftpRequest.GetRequestStream();
            /* Open a File Stream to Read the File for Upload */
            FileStream localFileStream = new FileStream(localFile, FileMode.OpenOrCreate);
            /* Buffer for the Downloaded Data */
            byte[] byteBuffer = new byte[bufferSize];
            int bytesSent = localFileStream.Read(byteBuffer, 0, bufferSize);
            /* Upload the File by Sending the Buffered Data Until the Transfer is Complete */
            try
            {
                while (bytesSent != 0)
                {
                    ftpStream.Write(byteBuffer, 0, bytesSent);
                    bytesSent = localFileStream.Read(byteBuffer, 0, bufferSize);
                }
            }
            catch (Exception ex) { return $"Error(ftp.upload): {ex}"; }
            /* Resource Cleanup */
            localFileStream.Close();
            ftpStream.Close();
            ftpRequest = null;
            return "ftp upload complete";
        }
        catch (Exception ex) { return $"Error(ftp.upload): {ex}"; }
    }

    /* Delete File */
    public void delete(string deleteFile)
    {
        try
        {
            /* Create an FTP Request */
            ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + deleteFile);
            /* Log in to the FTP Server with the User Name and Password Provided */
            ftpRequest.Credentials = new NetworkCredential(user, pass);
            /* When in doubt, use these options */
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;
            /* Specify the Type of FTP Request */
            ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;
            /* Establish Return Communication with the FTP Server */
            ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            /* Resource Cleanup */
            ftpResponse.Close();
            ftpRequest = null;
        }
        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        return;
    }

    /* Rename File */
    public void rename(string currentFileNameAndPath, string newFileName)
    {
        try
        {
            /* Create an FTP Request */
            ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + currentFileNameAndPath);
            /* Log in to the FTP Server with the User Name and Password Provided */
            ftpRequest.Credentials = new NetworkCredential(user, pass);
            /* When in doubt, use these options */
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;
            /* Specify the Type of FTP Request */
            ftpRequest.Method = WebRequestMethods.Ftp.Rename;
            /* Rename the File */
            ftpRequest.RenameTo = newFileName;
            /* Establish Return Communication with the FTP Server */
            ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            /* Resource Cleanup */
            ftpResponse.Close();
            ftpRequest = null;
        }
        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        return;
    }

    /* Create a New Directory on the FTP Server */
    public void createDirectory(string newDirectory)
    {
        try
        {
            /* Create an FTP Request */
            ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + newDirectory);
            /* Log in to the FTP Server with the User Name and Password Provided */
            ftpRequest.Credentials = new NetworkCredential(user, pass);
            /* When in doubt, use these options */
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;
            /* Specify the Type of FTP Request */
            ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
            /* Establish Return Communication with the FTP Server */
            ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            /* Resource Cleanup */
            ftpResponse.Close();
            ftpRequest = null;
        }
        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        return;
    }

    /* Get the Date/Time a File was Created */
    public string getFileCreatedDateTime(string fileName)
    {
        try
        {
            /* Create an FTP Request */
            ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + fileName);
            /* Log in to the FTP Server with the User Name and Password Provided */
            ftpRequest.Credentials = new NetworkCredential(user, pass);
            /* When in doubt, use these options */
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;
            /* Specify the Type of FTP Request */
            ftpRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;
            /* Establish Return Communication with the FTP Server */
            ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            /* Establish Return Communication with the FTP Server */
            ftpStream = ftpResponse.GetResponseStream();
            /* Get the FTP Server's Response Stream */
            StreamReader ftpReader = new StreamReader(ftpStream);
            /* Store the Raw Response */
            string fileInfo = null;
            /* Read the Full Response Stream */
            try { fileInfo = ftpReader.ReadToEnd(); }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            /* Resource Cleanup */
            ftpReader.Close();
            ftpStream.Close();
            ftpResponse.Close();
            ftpRequest = null;
            /* Return File Created Date Time */
            return fileInfo;
        }
        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        /* Return an Empty string Array if an Exception Occurs */
        return "";
    }

    /* Get the Size of a File */
    public string getFileSize(string fileName)
    {
        try
        {
            /* Create an FTP Request */
            ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + fileName);
            /* Log in to the FTP Server with the User Name and Password Provided */
            ftpRequest.Credentials = new NetworkCredential(user, pass);
            /* When in doubt, use these options */
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;
            /* Specify the Type of FTP Request */
            ftpRequest.Method = WebRequestMethods.Ftp.GetFileSize;
            /* Establish Return Communication with the FTP Server */
            ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            /* Establish Return Communication with the FTP Server */
            ftpStream = ftpResponse.GetResponseStream();
            /* Get the FTP Server's Response Stream */
            StreamReader ftpReader = new StreamReader(ftpStream);
            /* Store the Raw Response */
            string fileInfo = null;
            /* Read the Full Response Stream */
            try { while (ftpReader.Peek() != -1) { fileInfo = ftpReader.ReadToEnd(); } }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            /* Resource Cleanup */
            ftpReader.Close();
            ftpStream.Close();
            ftpResponse.Close();
            ftpRequest = null;
            /* Return File Size */
            return fileInfo;
        }
        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        /* Return an Empty string Array if an Exception Occurs */
        return "";
    }

    /* List Directory Contents File/Folder Name Only */
    public string[] directoryListSimple(string directory)
    {
        try
        {
            /* Create an FTP Request */
            ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + directory);
            /* Log in to the FTP Server with the User Name and Password Provided */
            ftpRequest.Credentials = new NetworkCredential(user, pass);
            /* When in doubt, use these options */
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;
            /* Specify the Type of FTP Request */
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
            /* Establish Return Communication with the FTP Server */
            ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            /* Establish Return Communication with the FTP Server */
            ftpStream = ftpResponse.GetResponseStream();
            /* Get the FTP Server's Response Stream */
            StreamReader ftpReader = new StreamReader(ftpStream);
            /* Store the Raw Response */
            string directoryRaw = null;
            /* Read Each Line of the Response and Append a Pipe to Each Line for Easy Parsing */
            try { while (ftpReader.Peek() != -1) { directoryRaw += ftpReader.ReadLine() + "|"; } }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            /* Resource Cleanup */
            ftpReader.Close();
            ftpStream.Close();
            ftpResponse.Close();
            ftpRequest = null;
            /* Return the Directory Listing as a string Array by Parsing 'directoryRaw' with the Delimiter you Append (I use | in This Example) */
            try { string[] directoryList = directoryRaw.Split("|".ToCharArray()); return directoryList; }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        }
        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        /* Return an Empty string Array if an Exception Occurs */
        return new string[] { "" };
    }

    /* List Directory Contents in Detail (Name, Size, Created, etc.) */
    public string[] directoryListDetailed(string directory)
    {
        try
        {
            /* Create an FTP Request */
            ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + directory);
            /* Log in to the FTP Server with the User Name and Password Provided */
            ftpRequest.Credentials = new NetworkCredential(user, pass);
            /* When in doubt, use these options */
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;
            /* Specify the Type of FTP Request */
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            /* Establish Return Communication with the FTP Server */
            ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            /* Establish Return Communication with the FTP Server */
            ftpStream = ftpResponse.GetResponseStream();
            /* Get the FTP Server's Response Stream */
            StreamReader ftpReader = new StreamReader(ftpStream);
            /* Store the Raw Response */
            string directoryRaw = null;
            /* Read Each Line of the Response and Append a Pipe to Each Line for Easy Parsing */
            try { while (ftpReader.Peek() != -1) { directoryRaw += ftpReader.ReadLine() + "|"; } }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            /* Resource Cleanup */
            ftpReader.Close();
            ftpStream.Close();
            ftpResponse.Close();
            ftpRequest = null;
            /* Return the Directory Listing as a string Array by Parsing 'directoryRaw' with the Delimiter you Append (I use | in This Example) */
            try { string[] directoryList = directoryRaw.Split("|".ToCharArray()); return directoryList; }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        }
        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        /* Return an Empty string Array if an Exception Occurs */
        return new string[] { "" };
    }
}