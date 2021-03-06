﻿namespace Superscribe.Tests.Owin
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using global::Owin;

    using Machine.Specifications;

    using Microsoft.Owin.Testing;

    using Superscribe.Owin.Components;
    using Superscribe.Owin.Engine;
    using Superscribe.Owin.Extensions;
    using Superscribe.Owin.Pipelining;

    public class FirstComponent
    {
        private readonly Func<IDictionary<string, object>, Task> next;

        public FirstComponent(Func<IDictionary<string, object>, Task> next)
        {
            this.next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            await environment.WriteResponse("before#1");
            await this.next(environment);
            await environment.WriteResponse("after#1");
        }
    }

    public class SecondComponent
    {
        private readonly Func<IDictionary<string, object>, Task> next;

        public SecondComponent(Func<IDictionary<string, object>, Task> next)
        {
            this.next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            await environment.WriteResponse("before#2");
            await this.next(environment);
            await environment.WriteResponse("after#2");
        }
    }

    public class ThirdComponent
    {
        private readonly Func<IDictionary<string, object>, Task> next;

        public ThirdComponent(Func<IDictionary<string, object>, Task> next)
        {
            this.next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            await environment.WriteResponse("before#3");
            await this.next(environment);
            await environment.WriteResponse("after#3");
        }
    }

    public class PadResponse
    {
        private readonly Func<IDictionary<string, object>, Task> next;

        private readonly string tag;

        public PadResponse(Func<IDictionary<string, object>, Task> next, string tag)
        {
            this.next = next;
            this.tag = tag;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            await environment.WriteResponse("<" + this.tag + ">");
            await this.next(environment);
            await environment.WriteResponse("</" + this.tag + ">");
        }
    }

    public abstract class PipelineTests
    {
        protected static TestServer owinTestServer;

        protected static HttpResponseMessage responseMessage;

        protected static HttpClient client;

        protected static IOwinRouteEngine engine;

        protected Establish context = () =>
        {
            owinTestServer = TestServer.Create(
                builder =>
                {
                    engine = OwinRouteEngineFactory.Create();
                    builder.Use(typeof(OwinRouter), builder, engine);
                });

            client = owinTestServer.HttpClient;
            client.DefaultRequestHeaders.Add("accept", "text/html");
        };
    }

    public class When_building_a_pipeline_with_a_single_element_given_one_middleware : PipelineTests
    {
        private Establish context = () => engine.Pipeline("Hello").Use<FirstComponent>();

        private Because of = () => responseMessage = client.GetAsync("http://localhost/Hello").Result;

        private It should_execute_the_final_function = () => responseMessage.Content.ReadAsStringAsync().Result.ShouldEqual("before#1after#1");

        private It should_return_200 = () => responseMessage.StatusCode.ShouldEqual(HttpStatusCode.OK);
    }

    public class When_building_a_pipeline_with_a_single_element_given_two_middleware : PipelineTests
    {
        private Establish context = () => engine.Pipeline("Hello").Use<FirstComponent>().Use<SecondComponent>();

        private Because of = () => responseMessage = client.GetAsync("http://localhost/Hello").Result;

        private It should_execute_the_final_function = () => responseMessage.Content.ReadAsStringAsync().Result.ShouldEqual("before#1before#2after#2after#1");

        private It should_return_200 = () => responseMessage.StatusCode.ShouldEqual(HttpStatusCode.OK);
    }

    public class When_building_a_pipeline_with_a_two_elements_given_two_funcs : PipelineTests
    {
        private Establish context = () =>
        {
            engine.Pipeline("Hello").Use<FirstComponent>();
            engine.Pipeline("Hello/World").Use<SecondComponent>();
        };

        private Because of = () => responseMessage = client.GetAsync("http://localhost/Hello/World").Result;

        private It should_execute_the_final_function = () => responseMessage.Content.ReadAsStringAsync().Result.ShouldEqual("before#1before#2after#2after#1");

        private It should_return_200 = () => responseMessage.StatusCode.ShouldEqual(HttpStatusCode.OK);
    }

    public abstract class When_building_a_pipeline_with_options : PipelineTests
    {
        private Establish context = () =>
            {
                engine.Pipeline("Hello").Use<FirstComponent>();
                engine.Pipeline("Hello/Foo").Use<SecondComponent>();
                engine.Pipeline("Hello/Bar").Use<ThirdComponent>();
            };
    }

    public class When_building_a_pipeline_with_options_invoking_the_first : When_building_a_pipeline_with_options
    {
        private Because of = () => responseMessage = client.GetAsync("http://localhost/Hello/Foo").Result;

        private It should_execute_the_final_function = () => responseMessage.Content.ReadAsStringAsync().Result.ShouldEqual("before#1before#2after#2after#1");

        private It should_return_200 = () => responseMessage.StatusCode.ShouldEqual(HttpStatusCode.OK);
    }

    public class When_building_a_pipeline_with_options_invoking_the_second : When_building_a_pipeline_with_options
    {
        private Because of = () => responseMessage = client.GetAsync("http://localhost/Hello/Bar").Result;

        private It should_execute_the_final_function = () => responseMessage.Content.ReadAsStringAsync().Result.ShouldEqual("before#1before#3after#3after#1");

        private It should_return_200 = () => responseMessage.StatusCode.ShouldEqual(HttpStatusCode.OK);
    }

    public class When_building_a_pipeline_with_middleware_that_takes_parameters : PipelineTests
    {
        private Establish context = () =>
            {
                engine.Pipeline("Pad").Use<PadResponse>("h1");
                engine.Pipeline("Pad/Response").Use<FirstComponent>();
            };

        private Because of = () => responseMessage = client.GetAsync("http://localhost/Pad/Response").Result;

        private It should_execute_the_final_function = () => responseMessage.Content.ReadAsStringAsync().Result.ShouldEqual("<h1>before#1after#1</h1>");

        private It should_return_200 = () => responseMessage.StatusCode.ShouldEqual(HttpStatusCode.OK);
    }
}
