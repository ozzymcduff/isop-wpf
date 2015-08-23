require 'albacore'

require 'rbconfig'
require 'nuget_helper'

$dir = File.join(File.dirname(__FILE__),'src')
$sln = File.join($dir, "Isop.Wpf.sln")

desc "Install missing NuGet packages."
task :restore do
  NugetHelper.exec("restore #{$sln}")
end

desc "build"
build :build => [:restore] do |msb|
  msb.prop :configuration, :Debug
  msb.prop :platform, "Any CPU"
  msb.target = :Rebuild
  msb.be_quiet
  msb.nologo
  msb.sln = $sln
end

build :build_release => [:restore] do |msb|
  msb.prop :configuration, :Release
  msb.prop :platform, "Any CPU"
  msb.target = :Rebuild
  msb.be_quiet
  msb.nologo
  msb.sln = $sln
end


task :default => ['build']

desc "test using console"
test_runner :test => [:build] do |runner|
  runner.exe = NugetHelper.nunit_path
  files = Dir.glob(File.join($dir, "Isop.Wpf.Tests", "**", "bin", "Debug", "Isop.Wpf.Tests.dll"))
  runner.files = files 
end
