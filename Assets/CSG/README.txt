*WARNING*: polychop branch is very much a work-in-progress, and does not yet work for any useful purpose. 
It contains a translation of Stan Melax's original C++ code ( http://www.melax.com/polychop ) to C# / Unity, but porting is not complete.

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

* Currently does nothing with the 'shared' field on Polygons. We could use this for tracking submeshes / materials ?
* Almost certainly could do with optimization (eg, some Lists could probably become Vertex[] or Polygon[] arrays).