﻿using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json;
using System.IO;
using Isop.Client.Transfer;
using Isop.Gui.ViewModels;
using FakeItEasy;
using Isop.Client.Json;
using Isop.Client;
using Isop.Gui.Adapters;
namespace Isop.Wpf.Tests
{
    [TestFixture]
    public class InvokeMethodUsingIsopClientTests
    {
        private IJSonHttpClient _jsonHttpClient;
        private IsopClient _isopClient;

        private Stream WithSomeNoise()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("Just some noise");
            stream.Position = 0;
            return stream;
        }

        private static Root RootModelFromSource()
        {
            return new Root
            {
                GlobalParameters = new[] { new Param { Name = "name", Type = typeof(string).FullName } },
                Controllers = new[]{
                    new Controller
                    {
                        Name="My", 
                        Methods=new []{
                            new Method{Name="Action", Parameters=new []{new Param{Name="name", Type=typeof(string).FullName}}},
                            new Method{Name="AnotherAction", Parameters=new []{new Param{Name="name", Type=typeof(string).FullName}}}
                        }
                    }
                }
            };
        }

        [SetUp]
        public void SetUp()
        {
            _jsonHttpClient = A.Fake<IJSonHttpClient>();
            _isopClient = new IsopClient(_jsonHttpClient, "http://localhost:666");
        }

        private RootViewModel RootVmWithMethodSelected()
        {
            A.CallTo(() => _jsonHttpClient.Request(A<Request>._))
                .Returns(new JsonResponse(System.Net.HttpStatusCode.OK, JsonConvert.SerializeObject(
                    RootModelFromSource())));

            var mt = _isopClient.GetModel().Result;
            var treemodel = new RootViewModel(new JsonClient(_isopClient), mt);

            treemodel.CurrentMethod = treemodel.Controllers.Single()
                .Methods.Single(m => m.Name == "Action");
            return treemodel;
        }

        [Test]
        public void Can_run_selected_method()
        {
            var treemodel = RootVmWithMethodSelected();
            var param1 = treemodel.CurrentMethod.Parameters.Single();
            param1.Value = "new value";
            A.CallTo(() => _jsonHttpClient.Request(A<Request>._))
                .Returns(new JsonResponse(System.Net.HttpStatusCode.OK,
                    WithSomeNoise()));

            var result = treemodel.Execute().Result;
        }

        [Test]
        public void Can_handle_type_conversion_error()
        {
            var treemodel = RootVmWithMethodSelected();
            var param1 = treemodel.CurrentMethod.Parameters.Single();
            param1.Value = "new value";
            A.CallTo(() => _jsonHttpClient.Request(A<Request>._))
                .Throws(new RequestException(System.Net.HttpStatusCode.BadRequest,
                    "[{\"ErrorType\":\"TypeConversionFailed\",\"Message\":\"TypeConversionFailed !!\", \"Argument\":\"Arg\"}]"));

            var result = treemodel.Execute().Result;
            Assert.That(result.Result, Is.EqualTo("TypeConversionFailed !!"));
        }

        [Test]
        public void Can_handle_missing_argument_error()
        {
            var treemodel = RootVmWithMethodSelected();
            var param1 = treemodel.CurrentMethod.Parameters.Single();
            param1.Value = "new value";
            A.CallTo(() => _jsonHttpClient.Request(A<Request>._))
                .Throws(new RequestException(System.Net.HttpStatusCode.BadRequest,
                    "[{\"ErrorType\":\"MissingArgument\",\"Message\":\"MissingArgument !!\", \"Argument\":\"Arg\"}]"));

            var result = treemodel.Execute().Result;
            Assert.That(result.Result, Is.EqualTo("MissingArgument !!"));
        }
    }
}
