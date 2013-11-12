RtvSloScraping
==============

Apliccation in .NET4 for scraping data from rtvslo.si and saving it in RDF triple store.

INSTRUCTIONS
============
- rebuild solution
- all setting are editable in config files (path to triple store)
- run RtvSlo.ScrapingSimulator for testing or if you want to run it as console application

WINDOWS SERVICE
===============
- http://msdn.microsoft.com/en-us/library/sd8zc8ha(v=vs.110).aspx
- build RtvSlo.WindowsService
- find installutil.exe on your computer (or use Visual Studio Command Prompt - it has installutil included)
- find RtvSlo.WindowsService.exe in bin folder
- open command prompt and run:
	- installutil RtvSlo.WindowsService.exe (for installing windows service on your computer)
	- installutil /u RtvSlo.WindowsService.exe (for uninstalling windows service from your computer)
