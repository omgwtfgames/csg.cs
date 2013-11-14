csg.cs
======

Constructive Solid Geometry (CSG) for Unity in C#

Direct port of https://github.com/timknip/csg.as (Actionscript 3) to C# / Unity.

Nice summary with pseudocode of the algorithm: https://www.andrew.cmu.edu/user/jackiey/resources/CSG/CSG_report.pdf


Copyright (c) 2011 Evan Wallace (http://madebyevan.com/), under the MIT license (original Javascript version, https://github.com/evanw/csg.js/).

Copyright (c) 2012 Tim Knip (http://floorplanner.com/), under the MIT license (AS3 port, https://github.com/evanw/csg.js/).

Copyright (c) 2013 Andrew Perry (http://omgwtfgames.com), under MIT license (C#/Unity port).


TODO
----
* Works, but adds a whole pile of verticies that don't need to be there. Find a way to simpify the mesh for tris that share a plane (maybe http://www.melax.com/polychop) ?
* Currently does nothing with the 'shared' field on Polygons. We could use this for tracking submeshes / materials ?
* Almost certainly could do with optimization (eg, some Lists could probably become Vertex[] or Polygon[] arrays).