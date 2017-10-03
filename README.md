# NIRS-Recording
Near Infrared Spectroscopy Recording Application


This is a multi-threaded application written in C# to receive data from a serial port every 1/50 sec, conduct specific processing on data and display it.
Supports multiple channels but only tested for up to 4 channels. Displays the channel signals in real time. In case you want to change it for your application, you only need to change the byte processing function to match your data transfer protocol. The byte processing happens right after the "connect" button is pressed and data transfer is initiated.

More info to come soon.
