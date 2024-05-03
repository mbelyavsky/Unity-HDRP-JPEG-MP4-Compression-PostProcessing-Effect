1. Add "JPEGCompression folder to your progect."
2. Find and add override post process effect to your global volume at the bottom. Should be called "JPEGCompression"
3. Add our post process shader in the Custom Post Process Orders list (Project Settings > Graphics > HDRP Global Settings) (After Post Process).
4. Insert "JPEGMP4Comression.compute" shader file into its plase in "JPEGCompression" global volume parameter. 
