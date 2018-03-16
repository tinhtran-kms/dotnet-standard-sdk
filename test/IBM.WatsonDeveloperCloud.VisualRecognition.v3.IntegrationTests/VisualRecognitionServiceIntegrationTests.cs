﻿/**
* Copyright 2017 IBM Corp. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
*/

using IBM.WatsonDeveloperCloud.VisualRecognition.v3.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IBM.WatsonDeveloperCloud.Util;
using Newtonsoft.Json;

namespace IBM.WatsonDeveloperCloud.VisualRecognition.v3.IntegrationTests
{
    [TestClass]
    public class VisualRecognitionServiceIntegrationTests
    {
        private VisualRecognitionService _service;
        private static string credentials = string.Empty;
        private static string _apikey;
        private static string _endpoint;
        private string _imageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/b/bb/Kittyply_edit1.jpg/1200px-Kittyply_edit1.jpg";
        private string _faceUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8d/President_Barack_Obama.jpg/220px-President_Barack_Obama.jpg";
        private string _localGiraffeFilePath = @"VisualRecognitionTestData/giraffe_to_classify.jpg";
        private string _localFaceFilePath = @"VisualRecognitionTestData/obama.jpg";
        private string _localGiraffePositiveExamplesFilePath = @"VisualRecognitionTestData/giraffe_positive_examples.zip";
        private string _giraffeClassname = "giraffe";
        private string _localTurtlePositiveExamplesFilePath = @"VisualRecognitionTestData/turtle_positive_examples.zip";
        private string _turtleClassname = "turtle";
        private string _localNegativeExamplesFilePath = @"VisualRecognitionTestData/negative_examples.zip";
        private string _createdClassifierName = "dotnet-standard-test-integration-classifier";
        AutoResetEvent autoEvent = new AutoResetEvent(false);

        #region Setup
        [TestInitialize]
        public void Setup()
        {
            Console.WriteLine(string.Format("\nSetting up test"));

            if (string.IsNullOrEmpty(credentials))
            {
                try
                {
                    credentials = Utility.SimpleGet(
                        Environment.GetEnvironmentVariable("VCAP_URL"),
                        Environment.GetEnvironmentVariable("VCAP_USERNAME"),
                        Environment.GetEnvironmentVariable("VCAP_PASSWORD")).Result;
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("Failed to get credentials: {0}", e.Message));
                }

                Task.WaitAll();

                var vcapServices = JObject.Parse(credentials);
                _endpoint = vcapServices["visual_recognition"]["url"].Value<string>();
                _apikey = vcapServices["visual_recognition"]["api_key"].Value<string>();
            }

            _service = new VisualRecognitionService(_apikey, "2016-05-20");
            _service.Client.BaseClient.Timeout = TimeSpan.FromMinutes(120);
        }
        #endregion

        #region General
        [TestMethod]
        public void Classify_Success()
        {
            using (FileStream fs = File.OpenRead(_localGiraffeFilePath))
            {
                var result = _service.Classify(fs as Stream, imagesFileContentType: "image/jpeg");

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Images);
                Assert.IsTrue(result.Images.Count > 0);
            }
        }

        [TestMethod]
        public void ClassifyURL_Success()
        {
            var result = _service.Classify(url: _imageUrl);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Images);
            Assert.IsTrue(result.Images.Count > 0);
        }
        #endregion

        #region Face
        [TestMethod]
        public void DetectFaces_Success()
        {
            using (FileStream fs = File.OpenRead(_localFaceFilePath))
            {
                var result = _service.DetectFaces(fs as Stream, null, "image/jpeg");

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Images);
                Assert.IsTrue(result.Images.Count > 0);
            }
        }

        [TestMethod]
        public void DetectFacesURL_Success()
        {
            var result = _service.DetectFaces(url: _faceUrl);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Images);
            Assert.IsTrue(result.Images.Count > 0);
        }
        #endregion

        #region Custom
        [TestMethod]
        public void TestClassifiers_Success()
        {
            var listClassifiersResult = ListClassifiers();

            Classifier createClassifierResult = null;
            using (FileStream positiveExamplesStream = File.OpenRead(_localGiraffePositiveExamplesFilePath), negativeExamplesStream = File.OpenRead(_localNegativeExamplesFilePath))
            {
                Dictionary<string, Stream> positiveExamples = new Dictionary<string, Stream>();
                positiveExamples.Add(_giraffeClassname, positiveExamplesStream);
                CreateClassifier createClassifier = new CreateClassifier(_createdClassifierName, positiveExamples, negativeExamplesStream);
                createClassifierResult = _service.CreateClassifier(createClassifier);
            }

            string createdClassifierId = createClassifierResult.ClassifierId;

            var getClassifierResult = GetClassifier(createdClassifierId);

            try
            {
                IsClassifierReady(createdClassifierId);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get classifier...");
            }
            autoEvent.WaitOne();

            Classifier updateClassifierResult = null;
            var retrainRetries = 0;
            try
            {
                using (FileStream positiveExamplesStream = File.OpenRead(_localTurtlePositiveExamplesFilePath))
                {
                    Dictionary<string, Stream> positiveExamples = new Dictionary<string, Stream>();
                    positiveExamples.Add(_turtleClassname, positiveExamplesStream);
                    UpdateClassifier updateClassifier = new UpdateClassifier(createdClassifierId, positiveExamples);
                    updateClassifierResult = _service.UpdateClassifier(updateClassifier);
                }
            }
            catch (Exception e)
            {
                retrainRetries++;

                if (retrainRetries < 3)
                {
                    Console.WriteLine("Retraining failed. Retrying...");
                    using (FileStream positiveExamplesStream = File.OpenRead(_localTurtlePositiveExamplesFilePath))
                    {
                        Dictionary<string, Stream> positiveExamples = new Dictionary<string, Stream>();
                        positiveExamples.Add(_turtleClassname, positiveExamplesStream);
                        UpdateClassifier updateClassifier = new UpdateClassifier(createdClassifierId, positiveExamples);
                        updateClassifierResult = _service.UpdateClassifier(updateClassifier);
                    }
                }
                else
                {
                    Console.WriteLine("Retraining failed. Out of retries...");
                }
            }

            try
            {
                IsClassifierReady(createdClassifierId);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get classifier...");
            }
            autoEvent.WaitOne();

            var deleteClassifierResult = DeleteClassifier(createdClassifierId);

            Assert.IsNotNull(deleteClassifierResult);
            Assert.IsNotNull(updateClassifierResult);
            Assert.IsTrue(updateClassifierResult.ClassifierId == createdClassifierId);
            Assert.IsNotNull(getClassifierResult);
            Assert.IsTrue(getClassifierResult.ClassifierId == createdClassifierId);
            Assert.IsNotNull(createClassifierResult);
            Assert.IsTrue(createClassifierResult.Name == _createdClassifierName);
            Assert.IsNotNull(listClassifiersResult);
        }
        #endregion

        #region Utility
        #region IsClassifierReady
        private void IsClassifierReady(string classifierId)
        {
            var getClassifierResponse = _service.GetClassifier(classifierId);

            Console.WriteLine(string.Format("Classifier status is {0}", getClassifierResponse.Status.ToString()));

            if (getClassifierResponse.Status == Classifier.StatusEnum.READY)
            {
                autoEvent.Set();
            }
            else if (getClassifierResponse.Status == Classifier.StatusEnum.FAILED)
            {
                throw new Exception("Classifier failed!");
            }
            else
            {
                Task.Factory.StartNew(() =>
                {
                    System.Threading.Thread.Sleep(10000);
                    try
                    {
                        IsClassifierReady(classifierId);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                });
            }
        }
        #endregion
        #endregion

        #region Generated
        #region Classify
        private ClassifiedImages Classify(System.IO.Stream imagesFile = null, string acceptLanguage = null, string url = null, float? threshold = null, List<string> owners = null, List<string> classifierIds = null, string imagesFileContentType = null)
        {
            Console.WriteLine("\nAttempting to Classify()");
            var result = _service.Classify(imagesFile: imagesFile, acceptLanguage: acceptLanguage, url: url, threshold: threshold, owners: owners, classifierIds: classifierIds, imagesFileContentType: imagesFileContentType);

            if (result != null)
            {
                Console.WriteLine("Classify() succeeded:\n{0}", JsonConvert.SerializeObject(result, Formatting.Indented));
            }
            else
            {
                Console.WriteLine("Failed to Classify()");
            }

            return result;
        }
        #endregion

        #region DetectFaces
        private DetectedFaces DetectFaces(System.IO.Stream imagesFile = null, string url = null, string imagesFileContentType = null)
        {
            Console.WriteLine("\nAttempting to DetectFaces()");
            var result = _service.DetectFaces(imagesFile: imagesFile, url: url, imagesFileContentType: imagesFileContentType);

            if (result != null)
            {
                Console.WriteLine("DetectFaces() succeeded:\n{0}", JsonConvert.SerializeObject(result, Formatting.Indented));
            }
            else
            {
                Console.WriteLine("Failed to DetectFaces()");
            }

            return result;
        }
        #endregion
        
        #region DeleteClassifier
        private object DeleteClassifier(string classifierId)
        {
            Console.WriteLine("\nAttempting to DeleteClassifier()");
            var result = _service.DeleteClassifier(classifierId: classifierId);

            if (result != null)
            {
                Console.WriteLine("DeleteClassifier() succeeded:\n{0}", JsonConvert.SerializeObject(result, Formatting.Indented));
            }
            else
            {
                Console.WriteLine("Failed to DeleteClassifier()");
            }

            return result;
        }
        #endregion

        #region GetClassifier
        private Classifier GetClassifier(string classifierId)
        {
            Console.WriteLine("\nAttempting to GetClassifier()");
            var result = _service.GetClassifier(classifierId: classifierId);

            if (result != null)
            {
                Console.WriteLine("GetClassifier() succeeded:\n{0}", JsonConvert.SerializeObject(result, Formatting.Indented));
            }
            else
            {
                Console.WriteLine("Failed to GetClassifier()");
            }

            return result;
        }
        #endregion

        #region ListClassifiers
        private Classifiers ListClassifiers(bool? verbose = null)
        {
            Console.WriteLine("\nAttempting to ListClassifiers()");
            var result = _service.ListClassifiers(verbose: verbose);

            if (result != null)
            {
                Console.WriteLine("ListClassifiers() succeeded:\n{0}", JsonConvert.SerializeObject(result, Formatting.Indented));
            }
            else
            {
                Console.WriteLine("Failed to ListClassifiers()");
            }

            return result;
        }
        #endregion
        
        #endregion
    }
}
