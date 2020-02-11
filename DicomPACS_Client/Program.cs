﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;
using Dicom.Network;
using System.Globalization;

namespace DicomPACS_Client
{
    static class DicomCtrl
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

        }


        public static bool DcmPrefixValidate(FileStream Dcmfs) //dcm to image remove
        {
            Dcmfs.Seek(128, SeekOrigin.Begin); // Premble 128 bytes. and Prefix 'D','I','C','M' 4 bytes.
            return (Dcmfs.ReadByte() == (byte)'D' &&
                    Dcmfs.ReadByte() == (byte)'I' &&
                    Dcmfs.ReadByte() == (byte)'C' &&
                    Dcmfs.ReadByte() == (byte)'M');
        }

        public static string DcmStringCheck(string Dcmfile)
        {
            var dcm = DicomFile.Open(Dcmfile);
            string pID = dcm.Dataset.Get<string>(DicomTag.PatientID);
            string pName = dcm.Dataset.Get<string>(DicomTag.PatientName);
            return pName;
        }

        /// <summary>
        /// kernel32 import for ini file input/output
        /// </summary>
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);


        


        /// <summary>
        /// TODO : 이미지 폴더를 통째로 dicom 파일로 만드는것(ini까지 포함해서)
        /// 를 만들어야함.
        /// </summary>
        /// <param name="ImageFileFolder"></param>
        /// <param name="TargetPath"></param>
        /// <returns></returns>
        public static void MakeDicominFolder(string ImageFileFolder)//TODO: need target path change
        {
            //나중에 다른 Dicom 파일이면 이걸 바꾸는걸 꼭 추가해야합니다.
            int sizeCOL=793, sizeROW=1122;

            List<string> dirs = new List<string>(Directory.EnumerateDirectories(ImageFileFolder));
            //all dirs find

            StringBuilder PATIENT_ID = new StringBuilder();
            StringBuilder PATIENT_NAME = new StringBuilder();
            StringBuilder PATIENT_SEX = new StringBuilder();
            StringBuilder PATIENT_BOD = new StringBuilder();
            StringBuilder STUDY_DATE = new StringBuilder();
            StringBuilder STUDY_TIME = new StringBuilder();
            StringBuilder STUDY_DESC = new StringBuilder();
            StringBuilder ACCESSION_NO = new StringBuilder();
            StringBuilder ORDER_CODE = new StringBuilder();
            StringBuilder FILE_CNT = new StringBuilder();
            StringBuilder REQUEST = new StringBuilder();
            StringBuilder SEND_RESULT = new StringBuilder();

            foreach (string dir in dirs)
            {
                /*
                try
                {
                */


                string existSettingIniStr = dir + @"\info.ini";
                FileInfo fileInfo = new FileInfo(existSettingIniStr);
                if (!fileInfo.Exists)
                {
                    Form1.lb1.Items.Add("infoINI not exist : " + dir + "[" + DateTime.Now + "]");
                    continue;
                }

                //Example : GetPrivateProfileString("WookoaSetting", "TopAlways", "", topAlways, topAlways.Capacity, "C:\\info.ini");
                //Example : WritePrivateProfileString("WookoaSetting", "ViewTray", "false", "C:\\info.ini");
                //not need dirs name
                GetPrivateProfileString("INFO", "PATIENT_ID", "", PATIENT_ID, PATIENT_ID.Capacity, dir + @"\info.ini");
                GetPrivateProfileString("INFO", "PATIENT_NAME", "", PATIENT_NAME, PATIENT_NAME.Capacity, dir + @"\info.ini");
                GetPrivateProfileString("INFO", "PATIENT_SEX", "", PATIENT_SEX, PATIENT_SEX.Capacity, dir + @"\info.ini");
                GetPrivateProfileString("INFO", "PATIENT_BOD", "", PATIENT_BOD, PATIENT_BOD.Capacity, dir + @"\info.ini");
                GetPrivateProfileString("INFO", "STUDY_DATE", "", STUDY_DATE, STUDY_DATE.Capacity, dir + @"\info.ini");
                GetPrivateProfileString("INFO", "STUDY_TIME", "", STUDY_TIME, STUDY_TIME.Capacity, dir + @"\info.ini");
                GetPrivateProfileString("INFO", "STUDY_DESC", "", STUDY_DESC, STUDY_DESC.Capacity, dir + @"\info.ini");
                GetPrivateProfileString("INFO", "ACCESSION_NO", "", ACCESSION_NO, ACCESSION_NO.Capacity, dir + @"\info.ini");
                GetPrivateProfileString("INFO", "ORDER_CODE", "", ORDER_CODE, ORDER_CODE.Capacity, dir + @"\info.ini");
                GetPrivateProfileString("INFO", "FILE_CNT", "", FILE_CNT, FILE_CNT.Capacity, dir + @"\info.ini");
                GetPrivateProfileString("INFO", "REQUEST", "", REQUEST, REQUEST.Capacity, dir + @"\info.ini");
                GetPrivateProfileString("INFO", "SEND_RESULT", "", SEND_RESULT, SEND_RESULT.Capacity, dir + @"\info.ini");

                if(REQUEST.ToString() != "1")
                {
                    Form1.lb1.Items.Add("Request num is : " + REQUEST.ToString() + "[" + DateTime.Now + "]");
                    continue;
                }


                if (SEND_RESULT.ToString() == "O")
                {
                    Form1.lb1.Items.Add("Already dcm sended : " + dir + "[" + DateTime.Now + "]");
                    continue;

                }

                List<string> imgFiles = new List<string>(Directory.EnumerateFiles(dir));

                DicomDataset dataset = new DicomDataset();
                FillDataset(dataset,
                    PATIENT_ID.ToString(), PATIENT_NAME.ToString(), PATIENT_SEX.ToString(), PATIENT_BOD.ToString(), STUDY_DATE.ToString(), STUDY_TIME.ToString(), STUDY_DESC.ToString(), ACCESSION_NO.ToString(), ORDER_CODE.ToString()); //TODO : change need priavate profile string


                bool imageDataSetFlag = false;

                DicomPixelData pixelData = DicomPixelData.Create(dataset, true); //TODO : bug fix dicompixeldata create
                foreach (string imgfile in imgFiles)
                {
                    if (string.Compare(imgfile.Substring(imgfile.Length - 3, 3), "png") == 0 )
                    {
                        
                    }
                    else if (string.Compare(imgfile.Substring(imgfile.Length - 3, 3), "jpg") == 0)
                    {

                    }
                    else
                    {
                        continue;
                    }



                    Bitmap bitmap = new Bitmap(imgfile);
                    bitmap = GetValidImage(bitmap);

                    int rows, columns;
                    byte[] pixels = GetPixels(bitmap, out rows, out columns);

                    MemoryByteBuffer buffer = new MemoryByteBuffer(pixels);


                    double ratioCol = sizeCOL / (double)columns;
                    double ratioRow = sizeROW / (double)rows;
                    

                    double ratio = Math.Min(sizeROW, sizeCOL);

                    int newWidth = (int)(sizeCOL * ratio);
                    int newHeight = (int)(sizeROW * ratio);

                    Size resize = new Size(newWidth, newHeight);
                    Bitmap resizeImage = new Bitmap(bitmap, resize);


                    if (imageDataSetFlag == false)
                    {
                        dataset.Add(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Rgb.Value);
                        dataset.Add(DicomTag.Rows, (ushort)sizeROW);
                        dataset.Add(DicomTag.Columns, (ushort)sizeCOL); //TODO : ADD Dcm image count check
                        imageDataSetFlag = true;
                    }

                    pixelData.BitsStored = 8;
                    pixelData.SamplesPerPixel = 3; // 3 : red/green/blue  1 : CT/MR Single Grey Scale
                    pixelData.HighBit = 7;
                    pixelData.PixelRepresentation = 0;
                    pixelData.PlanarConfiguration = 0;

                    pixelData.AddFrame(buffer);
                    //TODO : Need to check if it is created dcm in directory

                }

                DicomFile dicomfile = new DicomFile(dataset);

                //string TargetFile = Path.Combine(TargetPath, sopInstanceUID + ".dcm");
                string TargetFile = Path.Combine(dir, dataset.GetString(DicomTag.SOPInstanceUID) + ".dcm");

                dicomfile.Save(TargetFile); //todo : dicom file save error
                SendToPACS(TargetFile, Form1.tb2.Text, Form1.tb3.Text, int.Parse(Form1.tb4.Text), Form1.tb5.Text);


                WritePrivateProfileString("INFO", "SEND_RESULT", "O", dir + @"\info.ini");
                Form1.lb1.Items.Add("dcm send finish : " + dir + "[" + DateTime.Now + "]");
                /*
            }

            catch(Exception e)
            {
                Form1.lb1.Items.Add("Makedicomfolder error exception : " + e.Message);
                Form1.lb1.Items.Add(dir + "[" + DateTime.Now + "]");
            }
            */
            }

        }


        //안씀
        public static string MakeDicom(string ImageFile, string TargetPath)
        {
            Bitmap bitmap = new Bitmap(ImageFile);
            bitmap = GetValidImage(bitmap);

            int rows, columns;
            byte[] pixels = GetPixels(bitmap, out rows, out columns);

            MemoryByteBuffer buffer = new MemoryByteBuffer(pixels);

            DicomDataset dataset = new DicomDataset();
            //FillDataset(dataset);
            //TODO : make dicom test
            dataset.Add(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Rgb.Value);
            dataset.Add(DicomTag.Rows, (ushort)rows);
            dataset.Add(DicomTag.Columns, (ushort)columns);

            DicomPixelData pixelData = DicomPixelData.Create(dataset, true); //TODO : bug fix dicompixeldata create

            pixelData.BitsStored = 8;
            pixelData.SamplesPerPixel = 3; // 3 : red/green/blue  1 : CT/MR Single Grey Scale
            pixelData.HighBit = 7;
            pixelData.PixelRepresentation = 0;
            pixelData.PlanarConfiguration = 0;

            pixelData.AddFrame(buffer);
            pixelData.AddFrame(buffer); //TODO : 두개가 들어가는지 테스트

            DicomFile dicomfile = new DicomFile(dataset);

            //string TargetFile = Path.Combine(TargetPath, sopInstanceUID + ".dcm");
            string TargetFile = Path.Combine(TargetPath, "Test.dcm");

            dicomfile.Save(TargetFile); //todo : dicom file save error

            return TargetFile;
        }

        private static void FillDataset(DicomDataset dataset,
            string patientid, string patientname, string patientsex, string patientbod, string studydate, string studytime, string studydesc,
            string assessionno, string ordercode)

        {//bod = birthdate
            dataset.Add(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);
            dataset.Add(DicomTag.StudyInstanceUID, GenerateUid());  //스터디는 촬영 부위
            dataset.Add(DicomTag.SeriesInstanceUID, GenerateUid()); //예 : 이미지 10장을 묶는것 시리즈 상위 그룹이 있는듯?
            dataset.Add(DicomTag.SOPInstanceUID, GenerateUid());

            dataset.Add(DicomTag.BitsAllocated, "8");//add bit allocate but pixeldata delete

            dataset.Add(DicomTag.PatientID, patientid);

            dataset.Add(DicomTag.SpecificCharacterSet, "ISO 2022 IR 149");

            dataset.Add(DicomTag.PatientName, patientname);

            dataset.Add(DicomTag.PatientBirthDate, patientbod);



            dataset.Add(DicomTag.PatientSex, patientsex);
            /// A string of characters with one of the following formats
            /// -- nnnD, nnnW, nnnM, nnnY; where nnn shall contain the number of days for D, weeks for W, months for M, or years for Y.
            ///Example: "018M" would represent an age of 18 months.

            //TODO : Patient Age modify by birthday date


            DateTime theTime = DateTime.ParseExact(patientbod,
                                        "yyyyMMdd",
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.None);
            DateTime now = DateTime.Today;
            int age = now.Year - theTime.Year;
            if (now < DateTime.ParseExact(patientbod,
                                        "yyyyMMdd",
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.None).AddYears(age)) age--;

            string agefmt = "000";

            dataset.Add(DicomTag.PatientAge, age.ToString(agefmt) + "Y");  //TODO : Patient Age modify by birthday date


            dataset.Add(DicomTag.StudyDate, studydate);
            //dataset.Add(DicomTag.StudyDate, DateTime.Now);
            dataset.Add(DicomTag.StudyTime, studytime);
            //dataset.Add(DicomTag.StudyTime, DateTime.Now);
            dataset.Add(DicomTag.StudyDescription, studydesc);
            //추가된 내용
            dataset.Add(DicomTag.AccessionNumber, assessionno);
            //dataset.Add(DicomTag.AccessionNumber, string.Empty);

            //오더코드는 없는데...

            dataset.Add(DicomTag.ReferringPhysicianName, string.Empty);
            dataset.Add(DicomTag.StudyID, "1");
            dataset.Add(DicomTag.SeriesNumber, "1");
            dataset.Add(DicomTag.ModalitiesInStudy, "OT");
            dataset.Add(DicomTag.Modality, "OT");
            dataset.Add(DicomTag.NumberOfStudyRelatedInstances, "1");
            dataset.Add(DicomTag.NumberOfStudyRelatedSeries, "1");
            dataset.Add(DicomTag.NumberOfSeriesRelatedInstances, "1");
            dataset.Add(DicomTag.PatientOrientation, @"F\A"); //Patient direction of the rows and columns of the image 
            dataset.Add(DicomTag.ImageLaterality, "U");

            dataset.Add(DicomTag.ContentDate, DateTime.Now);
            dataset.Add(DicomTag.ContentTime, DateTime.Now);
            dataset.Add(DicomTag.InstanceNumber, "1");
            dataset.Add(DicomTag.ConversionType, "WSD"); //Describes the kind of image conversion.
        }

        private static DicomUID GenerateUid()
        {
            StringBuilder uid = new StringBuilder();
            //uid.Append("1.08.1982.10121984.2.0.07").Append('.').Append(DateTime.UtcNow.Ticks); //original
            uid.Append("1.2.840.10008").Append('.').Append(DateTime.UtcNow.Ticks); //change


            return new DicomUID(uid.ToString(), "SOP Instance UID", DicomUidType.SOPInstance);
        }

        private static Bitmap GetValidImage(Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb)
            {
                Bitmap old = bitmap;
                using (old)
                {
                    bitmap = new Bitmap(old.Width, old.Height, PixelFormat.Format24bppRgb);
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        g.DrawImage(old, 0, 0, old.Width, old.Height);
                    }
                }
            }
            return bitmap;
        }

        private static byte[] GetPixels(Bitmap image, out int rows, out int columns)
        {
            rows = image.Height;
            columns = image.Width;
            if (rows % 2 != 0 && columns % 2 != 0) --columns;

            BitmapData data = image.LockBits(new Rectangle(0, 0, columns, rows), ImageLockMode.ReadOnly, image.PixelFormat);
            IntPtr bmpData = data.Scan0;

            try
            {
                int stride = columns * 3;
                int size = rows * stride;
                byte[] pixelData = new byte[size];
                for (int i = 0; i < rows; ++i) Marshal.Copy(new IntPtr(bmpData.ToInt64() + i * data.Stride), pixelData, i * stride, stride);

                SwapRedBlue(pixelData);
                return pixelData;
            }
            finally
            {
                image.UnlockBits(data);
            }
        }
        private static void SwapRedBlue(byte[] pixel)
        {
            for (int i = 0; i < pixel.Length; i += 3)
            {
                byte temp = pixel[i];
                pixel[i] = pixel[i + 2];
                pixel[i + 2] = temp;
            }
        }

        //need button or code send to pacs
        public static void SendToPACS(string dcmfile, string sourceAET, string targetIP, int targetPort, string targetAET)
        {
            var m_pDicomFile = DicomFile.Open(dcmfile);

            DicomClient pClient = new DicomClient();

            pClient.NegotiateAsyncOps();
            pClient.AddRequest(new DicomCStoreRequest(m_pDicomFile, DicomPriority.Medium));
            pClient.Send(targetIP, targetPort, false, sourceAET, targetAET);



        }


    }





}


