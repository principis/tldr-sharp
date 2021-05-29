%global         debug_package %{nil}

Name:           tldr-sharp
Version:        VERSION_PLACEHOLDER
Release:        1%{?dist}
Summary:        C# tldr client
BuildArch:      noarch

License:        GPLv3
URL:            https://github.com/principis/%{name}
Source0:        https://github.com/principis/%{name}/archive/v%{version}/%{name}-%{version}.tar.gz

AutoReqProv:  no
BuildRequires:  mono-devel >= 4.8
BuildRequires:  nuget
Requires:       mono-core >= 4.8
Requires:       mono-data-sqlite >= 4.8
Requires:       sqlite


%global _libdir /usr/lib
%global _lib lib

%description
A C# based, feature-rich command-line client for tldr-pages.

%prep
%autosetup

%build
## nothing to build

%install

install -Dm755 "scripts/debian/tldr-sharp" %{buildroot}%{_bindir}/tldr-sharp
sed -i "s+/usr/lib/+%{_libdir}/+g" %{buildroot}%{_bindir}/tldr-sharp
pushd tldr-sharp/bin/Release
find . -not -name "*.exe" -type f -exec install -Dm 644 '{}' "%{buildroot}%{_libdir}/%{name}/{}" \;
find . -name '*.exe' -type f -exec install -Dm 755 '{}' "%{buildroot}%{_libdir}/%{name}/{}" \;
popd

%post
%{_sbindir}/update-alternatives --install %{_bindir}/tldr tldr %{_bindir}/%{name} 10

%postun
if [ $1 -eq 0 ] ; then
  %{_sbindir}/update-alternatives --remove tldr %{_bindir}/%{name}
fi

%files
%{_bindir}/%{name}
%{_libdir}/%{name}