using Microsoft.AspNetCore.SignalR;

public class DataHub1 : Hub
    {
        public async Task SendData(string data,string data1,string data2)
        {
            //write data to serial port
            System.Console.WriteLine("Listening to serial port");

    
            await Clients.All.SendAsync("ReceiveData", data,data1,data2);
        }
    }