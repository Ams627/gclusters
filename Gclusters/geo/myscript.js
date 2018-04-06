window.onload = () => {
    var maxX = Math.max(...coastline.features.map(x => Math.max(...x.geometry.coordinates.map(y => y[0]))));
    var maxY = Math.max(...coastline.features.map(x => Math.max(...x.geometry.coordinates.map(y => y[1]))));
    var minX = Math.min(...coastline.features.map(x => Math.min(...x.geometry.coordinates.map(y => y[0]))));
    var minY = Math.min(...coastline.features.map(x => Math.min(...x.geometry.coordinates.map(y => y[1]))));
    console.log(maxX);
    console.log(maxY);
    console.log(minX);
    console.log(minY);

    var scaleFactor = 400;

    function scaleX(num) {
        var temp = ((num - minX) * scaleFactor) / (maxX - minX);
        return Math.round(temp * 100) / 100;
    }
    function scaleY(num) {
        var temp = ((maxY - (num - minY)) * scaleFactor) / (maxY - minY);
        return Math.round(temp * 100) / 100;
    }

    var svgns = "http://www.w3.org/2000/svg";

    function addPath(svg, pathD) {
        var path = document.createElementNS(svgns, "path");
        path.setAttributeNS(null, "d", pathD);
        svg.appendChild(path);
    }

    function getRandomColor() {
        var letters = '0123456789ABCDEF';
        var color = '#';
        for (var i = 0; i < 6; i++) {
            color += letters[Math.floor(Math.random() * 16)];
        }
        return color;
    }

    function addCircle(svg, x, y, title, colour) {
        var shape = document.createElementNS(svgns, "circle");
        shape.setAttributeNS(null, "cx", scaleX(x));
        shape.setAttributeNS(null, "cy", scaleY(y));
        shape.setAttributeNS(null, "r", 1);
        shape.setAttributeNS(null, "fill", colour);
        shape.setAttributeNS(null, "stroke", "none");

        var titleElement = document.createElementNS(svgns, "title");
        titleElement.textContent = title;
        shape.appendChild(titleElement);

        svg.appendChild(shape);
    }

    function addCircles(svg, clusterInfo) {
        for (var key in clusterInfo) {
            var colour = getRandomColor();
            var list = clusterInfo[key];
            for (var i = 0; i < list.length; i++) {
                addCircle(svg, list[i].Item1, list[i].Item2, colour);
            }
        }
    }

    function addCircles2(svg, info) {
        for (var key in info) {
            var colour = getRandomColor();
            var list = info[key];
            for (var i = 0; i < list.length; i++) {
                addCircle(svg, list[i].Eastings, list[i].Northings, `${key} ${list[i].Name} (${list[i].Crs} ${list[i].Nlc})`, colour);
            }
        }
    }


    var coastElement = document.querySelector("#ukmap");
    var railwaysElement = document.querySelector("#ukrailways");
    coastline.features.forEach(x => addPath(coastElement, `M${scaleX(x.geometry.coordinates[0][0])} ${scaleY(x.geometry.coordinates[0][1])} ${x.geometry.coordinates.slice(1).map(x => "L" + scaleX(x[0]) + " " + scaleY(x[1])).join(' ')}`));
//    console.log(railways);
    railways.geometries.forEach(x => addPath(railwaysElement, `M${scaleX(x.coordinates[0][0])} ${scaleY(x.coordinates[0][1])} ${x.coordinates.slice(1).map(x => "L" + scaleX(x[0]) + " " + scaleY(x[1])).join(' ')}`));
    //console.log(clusterInfo);

    var circles = document.querySelector("#circles");
    addCircles2(circles, stationInfo);

    var svgElement = document.querySelector('#map');
//    svgPanZoom(svgElement).s
    //svgPanZoom.setOnZoom(() => console.log(svgPanZoom.getZoom()));


    var radii = [3, 3, 3, 2, 1, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5];

    svgPanZoom(svgElement, {
        viewportSelector: '.svg-pan-zoom_viewport'
        , panEnabled: true
        , controlIconsEnabled: false
        , zoomEnabled: true
        , dblClickZoomEnabled: true
        , mouseWheelZoomEnabled: true
        , preventMouseEventsDefault: true
        , zoomScaleSensitivity: 0.2
        , minZoom: 0.1
        , maxZoom: 200
        , fit: true
        , contain: false
        , center: true
        , refreshRate: 'auto'
        , beforeZoom: function () { }
        , onZoom: function () {
            var zoom = this.getZoom();
            var svg = document.querySelector('#map');
            var circleElements = svg.getElementsByTagName("circle");

            var radius = 1 / zoom;
            if (radius > 1) {
                radius = 1;
            }
            else if (radius < 0.2) {
                radius = 0.2;
            }
            console.log(`radius ${radius} zoom ${zoom}`);
            //for (var i = radii.length; i >= 0; i--)
            //{
            //    if (zoom * 3 > i) {
            //        radius = radii[i];
            //        console.log(`radius ${radius} zoom ${zoom}`);
            //        break;
            //    }
            //}
            //for (var j = 0; j < circleElements.length; j++) {
            //    console.log(circleElements[j].getAttribute("r"));
            //}
            var array = Array.prototype.slice.call(circleElements);
            array.forEach(x => x.setAttribute("r", radius));
        }
        , beforePan: function () { }
        , onPan: function () { }
        , onUpdatedCTM: function () { }
        , eventsListenerElement: null
    });


}