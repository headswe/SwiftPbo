he_fnc_road = {
private ["_startpos","_done","_road","_doneRoads","_paths","_points","_attempt","_ret"];
	_startpos = _this select 0;
	_endpos = _this select 1;





	_done = false;

	_road = objNull;
	_doneRoads = [];
	_paths = [];
	_points = [];
	if(isOnRoad _startpos) then {_road = ((_startpos nearRoads 5) select 0)};
	_attempt = 0;
	while {!_done} do {
		if(_attempt >= 10) exitWith {};
		_ret = [_road,_doneRoads] call he_fnc_pathfind;
		_doneRoads = _doneRoads + (_doneRoads arrayIntersect (_ret select 1));
		_paths pushBack (_ret select 0);
		_done = _ret select 2;
		_attempt = _attempt +1;
	};
	paths = _paths;
};

he_fnc_pathfind = {
	private ["_startroad","_doneRoads","_road","_color","_points","_done","_roads","_distance","_cloestRoad","_oldroad","_r","_foreachindex","_reversePoints","_index","_d","_nr","_markerstr"];
_startroad = _this select 0;
	_doneRoads = _this select 1;
	_road = _startroad;
	_color = ["ColorBlack","ColorGreen","ColorBlue","ColorRed","ColorYellow"] call bis_fnc_selectRandom;
	_points = [];
	_done = false;
	while{!_done} do {
		_points pushBack (_road);
		_roads = roadsConnectedTo _road;
		_distance = 999999999999999999999999999999999999999999999999999;
		_cloestRoad = objNull;
		_roads = _roads - _doneRoads;
				_doneRoads pushBack _road;
		if(count _roads == 0) then {
			systemChat "oh dear!";

			_oldroad = _road;
			_road = objNull;


			_r = (getpos _oldroad) nearRoads 50;
			_r = _r - _doneRoads;
			{
				_r set [_foreachindex,[([_x,_oldroad] call BIS_fnc_distance2D),_x]];
			} forEach _r;
			_r sort true;
			if(count _r > 0) then {
				_road = (_r select 0) select 1;
				_roads = roadsConnectedTo _road;
				_roads = _roads - _doneRoads;

			};


			if(isNull _road) then
			{
				_reversePoints = _points;
				reverse _reversePoints;
				_index = -1;
				_points = [];
				_deletedpoints = [];
				{
					_r = roadsConnectedTo _x;
					if({!(_x in _doneRoads)} count _r > 0) exitWith {_road = _x;_roads = _r - _doneRoads;_index = _foreachindex;systemChat "found road"};
					_deletedpoints pushBack _x;
				} forEach _reversePoints;
				_points = _points - _deletedpoints;
			};


		};

		if(count _roads <= 0) exitWith {};

		{
			_d = [(getpos _x), _endpos] call BIS_fnc_distance2D;
			_nr = roadsConnectedTo _x;
			systemChat format ["Options : %1 | NR:%2 | D:%3",count _roads,count _nr,_d];
			if(_d < _distance) then {_cloestRoad = _x;_distance = _d;systemChat "found road";};
		} foreach _roads;

		if(isNull _cloestRoad && count _roads > 0) then {_cloestRoad = _roads select 0};
		if(isNull _cloestRoad) exitWith {};


		if([_cloestRoad, _endpos] call BIS_fnc_distance2D > [_road,_endpos] call BIS_fnc_distance2D &&  [_road, _endpos] call BIS_fnc_distance2D < 25) exitWith {_done = true;systemChat "done!"};





		_road = _cloestRoad;
		_markerstr = createMarker [str (random 1000),getpos _road];
		_markerstr setMarkerShape "ICON";
		_markerstr setMarkerColor _color;
		_markerstr setMarkerType "MIL_DOT";
	};

	[_points,_doneRoads,_done]
};
onMapSingleClick "'end' setmarkerpos _pos;  [getpos player,getMarkerPos 'end'] spawn he_fnc_road;";
