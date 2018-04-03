window.onload = () => {
    var maxIndex = 319;
    var ar = coastline.features[319].geometry.coordinates;
    
    var svg1 = `<svg><path fill="none" stroke="blue" stroke-width="100" transform="scale(0.001) scale(1,-1)" d="M${ar[0][0]} ${ar[0][1]} ${ar.slice(1).map(x => "L" + x[0] + " " + x[1]).join(' ')}"/></svg>`;
    document.body.innerHTML = svg1;
    console.log(svg1);
}