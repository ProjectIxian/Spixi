# Instructions and notes for Spixi translators:
- Reference language file is en-us.txt
- Language files must be saved as UTF-8 with BOM text file. It's best if you copy en-us.txt to a new file and start translating there
- lines that start with a semicolon (;) are comments/notes and should not be translated
- Language files use a simple key = value approach
- Make sure to translate only values, keys must NEVER be translated
- <br> or <br/> or <br /> means new line
- {0}, {1}, {2}, ... means parameters will be passed in by Spixi - every translation must ALWAYS have the same number of parameters as the original text, they can be placed anywhere in the text (where it makes sense)
