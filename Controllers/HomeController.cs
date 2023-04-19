using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using hardware.Models;
using MongoDB.Driver;
using System.IO.Ports;
using Microsoft.AspNetCore.SignalR;

namespace hardware.Controllers;


public class HomeController : Controller
{
    private readonly IHubContext<DataHub> _hubContext;
      private readonly IHubContext<DataHub1> _hubContext1;


    public HomeController(IHubContext<DataHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public IActionResult Index()
    {


        return View();
    }

    public IActionResult privacy()
    {
        return View();
    }


    public IActionResult Gauge()
    {
        // TODO: Your code here
        return View();
    }
    public IActionResult Gaugetwo()
    {
        // TODO: Your code here
        return View();
    }


    public async Task StartReading()
    {
        ///dev/ttyUSB0
        using (SerialPort serialPort = new SerialPort("COM10"))
        {
            serialPort.BaudRate = 19200;
            serialPort.Parity = Parity.Odd;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;

            serialPort.Open();

            byte[] tx_Data = new byte[10];
            tx_Data[0] = 0xFA;
            tx_Data[1] = 0x0A;
            tx_Data[2] = 0x03;
            tx_Data[3] = 0x01;
            tx_Data[4] = 0x01;
            tx_Data[5] = 0x00;
            tx_Data[6] = 0x00;
            tx_Data[7] = 0x00;
            tx_Data[8] = 0x00;
            tx_Data[9] = 0x0F;
            serialPort.Write(tx_Data, 0, tx_Data.Length);

            byte[] buffer = new byte[17];
            string data = "0";
            string data1 = "0";
            string data2 = "0";


            while (true)
            {
                while (serialPort.BytesToRead > 14)
                {
                    buffer[1] = (byte)serialPort.ReadByte();

                    if (buffer[1] == 0xFA) //250
                    {

                        buffer[2] = (byte)serialPort.ReadByte();
                        buffer[3] = (byte)serialPort.ReadByte();
                        serialPort.Read(buffer, 4, 10);

                        if (buffer[2] == 13 && buffer[4] == 4 && buffer[5] == 132)
                        {
                            Task.Run(() =>
                            {
                                if (buffer[10] == 255) data = "30";
                                else data = (buffer[10] * 3).ToString();
                            }).Wait();
                        }

                        if (buffer[2] == 17 && buffer[4] == 4 && buffer[5] == 133)
                        {
                            int pulse = (buffer[11] << 8) | buffer[10];
                            int spo = buffer[12];
                            if (pulse == 511) Task.Run(() => data1 = null).Wait();
                            else Task.Run(() => data1 = (pulse).ToString()).Wait();
                            if (spo == 127) Task.Run(() => data2 = (null)).Wait();
                            else Task.Run(() => data2 = (spo).ToString()).Wait();
                        }

                    }


                    //add to db
                    //await DbAsync(int.Parse(data), int.Parse(data1), int.Parse(data2));


                    await _hubContext.Clients.All.SendAsync("ReceiveData", data, data1, data2);



                }
            }


        }
    }


    //db connection
    public async Task DbAsync(int chart, int pulse, int spo)
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("hardware");
        var collection = database.GetCollection<mongodb>("info");
        var document = new mongodb { chart = chart, pulse = pulse, spo = spo, Datetime = DateTime.Now };
        await collection.InsertOneAsync(document);
    }

    //get data
    public async Task GetData()
    {
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("hardware");
            var collection = database.GetCollection<mongodb>("info");
            var pageSize = 1;
            var pageNumber = 1;
            var filter = Builders<mongodb>.Filter.Empty;
            var options = new FindOptions<mongodb> { Sort = Builders<mongodb>.Sort.Ascending(d => d.Datetime) };

            var documents = new List<mongodb>();
            var cursor = await collection.FindAsync(filter, options);
            while (await cursor.MoveNextAsync())
            {
                var batch = cursor.Current;
                foreach (var document in batch)
                {
                    if (documents.Count < pageSize * 1)
                    {
                           await _hubContext.Clients.All.SendAsync("ReceiveData", document.chart.ToString(), document.pulse.ToString(), document.spo.ToString(),document.Datetime.ToString());
                          //sleep
                            await Task.Delay(10);
                    }
                    else
                    {
                        pageNumber++;
                    }
                }
            }


       

    }


 




}
