﻿using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RabbitQueue;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LettuceService
{
  class LettuceBin
  {
    private static Queue _queue;

    static async Task Main(string[] args)
    {
      var config = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

      if (_queue == null)
        _queue = new Queue(config["rabbitmq:url"], "lettucebin");

      Console.WriteLine("### Lettuce bin service starting to listen");
      _queue.StartListening(HandleMessage);

      // wait forever - we run until the container is stopped
      await new AsyncManualResetEvent().WaitAsync();
    }

    private volatile static int _inventory = 0;

    private static void HandleMessage(BasicDeliverEventArgs ea, string message)
    {
      var request = JsonConvert.DeserializeObject<Messages.LettuceBinRequest>(message);
      var response = new Messages.LettuceBinResponse();
      lock (_queue)
      {
        if (request.Returning)
        {
          Console.WriteLine($"### Request for {request.GetType().Name} - returned");
          _inventory++;
        }
        else if (_inventory > 0)
        {
          Console.WriteLine($"### Request for {request.GetType().Name} - filled");
          _inventory--;
          response.Success = true;
          _queue.SendReply(ea.BasicProperties.ReplyTo, ea.BasicProperties.CorrelationId, response);
        }
        else
        {
          Console.WriteLine($"### Request for {request.GetType().Name} - no inventory");
          response.Success = false;
          _queue.SendReply(ea.BasicProperties.ReplyTo, ea.BasicProperties.CorrelationId, response);
        }
      }
    }
  }
}
