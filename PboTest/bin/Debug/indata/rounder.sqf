//////////
// precison
// Credits : Sebatian Broberg
//////

_dotNum = toArray(".") select 0;
_num = _this select 0;
_precision = 2;
_string = format ["%1",_num];
_strArray = toArray(_string);
_index = _strArray find _dotNum;
_newStrArray = [];
if(_index > 0) then
{
	for "_x" from 0 to (_index+_precision) do
	{
		_newStrArray += [_x];
	};

};
_str = toString(_newStrArray);
_str