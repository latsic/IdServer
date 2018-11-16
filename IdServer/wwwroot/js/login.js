
$(document).ready(function () {
  
  var userName = $("#Username");
  var password = $("#Password");
  var login = $("#login")
  var input = $("input");
  
  function canRegister()
  {
    return userName.val().length > 0 && password.val().length;
  }

  function setEnabled()
  {
    if(canRegister())
    {
      login.removeAttr("disabled");
    }
    else
    {
      login.attr("disabled", "disabled");
    }
  }

  setEnabled();

  input.keyup(function(){
    setEnabled();
  });

  input.change(function(){
    setEnabled();
  })

});
