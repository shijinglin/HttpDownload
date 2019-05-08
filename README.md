# HttpDownload  -  HttpListener
Close the Shared Port for Simple Download

因为勒索病毒，被迫关闭共享端口，用EXE实现简单的HTTP下载，免于搭建Web服务器.

用法：将它放在您需要共享的目录并启动

支持自定义端口，自动启动：-a
支持文件夹快捷方式，将其他需要下载的目录快捷方式放到有Download程序的目录下即可
端口如果被占用，则自动往后寻找可用端口，超出50个不能用，则提示换端口。